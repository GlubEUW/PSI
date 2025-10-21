using System.Collections.Concurrent;
using Api.Models;

namespace Api.Services;

public class LobbyService() : ILobbyService
{
   private static ConcurrentDictionary<string, MatchSession> _sessions = new();

   public bool AddGameId(string code, string playerName, string gameId = "")
   {
      if (string.IsNullOrEmpty(gameId))
         gameId = code;

      if (_sessions.TryGetValue(code, out var session) && session is not null)
         return session._gameIdByPlayerName.TryAdd(playerName, gameId);

      return false;
   }

   public bool RemoveGameId(string? code, string? playerName)
   {
      if (code is null || playerName is null)
         return false;

      if (_sessions.TryGetValue(code, out var session) && session is not null)
         return session._gameIdByPlayerName.Remove(playerName);

      return false;
   }

   public bool TryGetGameId(string? code, string? playerName, out string? gameId)
   {
      if (code is null || playerName is null)
      {
         gameId = null;
         return false;
      }

      if (_sessions.TryGetValue(code, out var session) && session is not null)
      {
         if (session._gameIdByPlayerName.TryGetValue(playerName, out var gameID) && gameID is not null)
         {
            gameId = gameID;
            return true;
         }
      }
      gameId = null;
      return false;
   }


   public List<string> GetPlayersInLobby(string code)
   {
      if (_sessions.TryGetValue(code, out var session) && session is not null)
         return new List<string>(session.Players);

      return new List<string>();
   }

   public LobbyInfoDto GetLobbyInfo(string code)
   {
      return new LobbyInfoDto
      {
         Players = GetPlayersInLobby(code)
      };
   }

   public Task<bool> CreateMatch(string code)
   {
      if (!_sessions.ContainsKey(code))
      {
         _sessions[code] = new MatchSession
         {
            Code = code,
            Players = new List<string>(2),
            InGame = false
         };
         return Task.FromResult(true);
      }
      return Task.FromResult(false);
   }

   public Task<string?> JoinMatch(string code, string playerName)
   {
      if (!_sessions.TryGetValue(code, out var session))
      {
         return Task.FromResult<string?>("Game does not exist.");
      }
      var error = CanJoinLobby(code, playerName);
      if (error is null)
      {
         session.Players.Add(playerName);
      }
      return Task.FromResult(error);
   }

   public Task<bool> LeaveMatch(string code, string playerName)
   {
      if (_sessions.TryGetValue(code, out var session))
      {
         session.Players.Remove(playerName);
         if (session.Players.Count == 0)
         {
            return Task.FromResult(_sessions.TryRemove(code, out _));
         }
         return Task.FromResult(true);
      }
      return Task.FromResult(false);
   }

   public string? CanJoinLobby(string code, string playerName)
   {
      if (_sessions.TryGetValue(code, out var session) && session != null)
      {
         if (session.InGame)
         {
            return "Game already started.";
         }
         if (session.Players.Count >= session.Players.Capacity)
         {
            return "Lobby is full.";
         }
         if (session.Players.Contains(playerName))
         {
            return "Name already taken.";
         }
         return null;
      }
      return "Game does not exist.";
   }

   public Task<string> CreateLobbyWithSettings(int numberOfPlayers, int numberOfRounds, bool randomGames, List<string>? gamesList)
   {
      string code;
      do
      {
         code = GenerateUniqueLobbyCode();
      } while (_sessions.ContainsKey(code));

      List<string> finalGamesList;

      if (randomGames || gamesList == null || gamesList.Count == 0)
      {
         finalGamesList = GenerateRandomGames(numberOfRounds);
      }
      else
      {
         finalGamesList = gamesList;
      }

      _sessions[code] = new MatchSession
      {
         Code = code,
         Players = new List<string>(numberOfPlayers),
         GamesList = finalGamesList,
         InGame = false
      };
      return Task.FromResult(code);
   }
   public MatchSession? GetMatchSession(string code)
   {
      if (_sessions.TryGetValue(code, out var session))
         return session;
      return null;
   }


   private List<string> GenerateRandomGames(int count)
   {
      var availableGames = new[] { "TicTacToe", "RockPaperScissors" };
      var random = new Random();
      var games = new List<string>();

      for (int i = 0; i < count; i++)
      {
         games.Add(availableGames[random.Next(availableGames.Length)]);
      }

      return games;
   }

   private string GenerateUniqueLobbyCode()
   {
      var random = new Random();
      string code;

      do
      {
         code = random.Next(1000, 9999).ToString();
      }
      while (_sessions.ContainsKey(code));

      return code;
   }
}
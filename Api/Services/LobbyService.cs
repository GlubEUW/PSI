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
         CreateMatch(code);
         session = _sessions[code];
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

   public Task<string> CreateLobbyWithSettings(int numberOfRounds, List<string> gamesList, int maxPlayers)
   {
      string code;
      do
      {
         code = GenerateUniqueLobbyCode();
      } while (_sessions.ContainsKey(code));

      _sessions[code] = new MatchSession
      {
         Code = code,
         Players = new List<string>(maxPlayers),  // Set capacity
         NumberOfRounds = numberOfRounds,
         GamesList = gamesList,
         MaxPlayers = maxPlayers,
         inGame = false
      };
      return Task.FromResult(code);
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
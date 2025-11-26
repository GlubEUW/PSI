using System.Collections.Concurrent;
using Api.Models;
using Api.Entities;
using Api.GameLogic;

namespace Api.Services;

public class LobbyService(IGameFactory gameFactory) : ILobbyService
{
   private static ConcurrentDictionary<string, TournamentSession> _sessions = new();
   private readonly IGameFactory _gameFactory = gameFactory;
   public bool AddGameId(string code, Guid userId, string gameId = "")
   {
      if (string.IsNullOrEmpty(gameId))
         gameId = code;

      if (_sessions.TryGetValue(code, out var session) && session is not null)
         return session.GamesByPlayers.TryAdd(userId, gameId);

      return false;
   }

   public bool RemoveGameId(string? code, Guid? userId)
   {
      if (code is null || userId is null)
         return false;

      if (_sessions.TryGetValue(code, out var session) && session is not null)
         return session.GamesByPlayers.Remove((Guid)userId);

      return false;
   }

   public bool TryGetGameId(string? code, Guid userId, out string? gameId)
   {
      if (code is null)
      {
         gameId = null;
         return false;
      }

      if (_sessions.TryGetValue(code, out var session) && session is not null)
      {
         if (session.GamesByPlayers.TryGetValue(userId, out var gameID) && gameID is not null)
         {
            gameId = gameID;
            return true;
         }
      }
      gameId = null;
      return false;
   }

   public List<User> GetPlayersInLobby(string code)
   {
      if (_sessions.TryGetValue(code, out var session) && session is not null)
         return session.Players;

      return new List<User>();
   }
   public bool AreAllPlayersInLobby(string code)
   {
      if (!_sessions.TryGetValue(code, out var session))
         return false;

      return session.Players.All(p => !session.GamesByPlayers.ContainsKey(p.Id));
   }

   public Task<string?> JoinTournament(string code, User user)
   {
      if (!_sessions.TryGetValue(code, out var session))
         return Task.FromResult<string?>("Game does not exist.");

      var error = CanJoinLobby(code, user.Id);
      if (error is null)
         session.Players.Add(user);

      return Task.FromResult(error);
   }

   public Task<bool> LeaveMatch(string code, Guid userId)
   {
      if (_sessions.TryGetValue(code, out var session))
      {
         var user = session.Players.FirstOrDefault(u => u.Id == userId);
         if (user is not null)
            session.Players.Remove(user);

         if (session.Players.Count == 0)
            return Task.FromResult(_sessions.TryRemove(code, out _));

         return Task.FromResult(true);
      }
      return Task.FromResult(false);
   }

   public string? CanJoinLobby(string code, Guid userId)
   {
      if (_sessions.TryGetValue(code, out var session) && session != null)
      {
         if (session.TournamentStarted)
            return "Game already started.";

         if (session.Players.Count >= session.Players.Capacity)
            return "Lobby is full.";

         if (session.Players.Any(u => u.Id == userId))
            return "Name already taken.";

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
         finalGamesList = GenerateRandomGames(numberOfRounds);

      else
         finalGamesList = gamesList;

      _sessions[code] = new TournamentSession
      {
         Code = code,
         Players = new List<User>(numberOfPlayers),
         GameTypesByRounds = finalGamesList,
         NumberOfRounds = numberOfRounds,
         TournamentStarted = false
      };
      return Task.FromResult(code);
   }

   public TournamentSession? GetTournamentSession(string code)
   {
      if (_sessions.TryGetValue(code, out var session))
         return session;

      return null;
   }

   private List<string> GenerateRandomGames(int count)
   {
      var availableGames = _gameFactory.ValidGameTypes.ToArray();
      var random = new Random();
      var games = new List<string>();

      for (var i = 0; i < count; i++)
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

   public RoundInfoDto GetTournamentRoundInfo(string code)
   {
      if (!_sessions.TryGetValue(code, out var session) || session is null)
      {
         return new RoundInfoDto(1, 1);
      }

      var numberOfRounds = session.NumberOfRounds > 0 ? session.NumberOfRounds : 1;
      var currentRound = session.CurrentRound + 1;
      if (currentRound > numberOfRounds) currentRound = numberOfRounds;

      return new RoundInfoDto(currentRound, numberOfRounds);
   }

   public void MarkGameAsEnded(string code, string gameId)
   {
      if (_sessions.TryGetValue(code, out var session))
      {
         var round = session.CurrentRound;

         if (!session.EndedGamesByRound.TryGetValue(round, out var endedSet))
         {
            endedSet = new HashSet<string>();
            session.EndedGamesByRound[round] = endedSet;
         }

         lock (endedSet)
         {
            endedSet.Add(gameId);
         }

         var playersToRemove = session.GamesByPlayers
             .Where(kvp => kvp.Value == gameId)
             .Select(kvp => kvp.Key)
             .ToList();

         foreach (var playerId in playersToRemove)
            session.GamesByPlayers.Remove(playerId);

         session.TournamentStarted = false;
      }
   }


   public bool AreAllGamesEnded(string code)
   {
      if (_sessions.TryGetValue(code, out var session))
      {
         var round = session.CurrentRound;
         if (!session.EndedGamesByRound.TryGetValue(round, out var endedSet))
            return false;

         var allGameIds = session.GamesByPlayers.Values
             .Where(id => id.Contains($"_R{round}"))
             .Distinct()
             .ToList();

         return allGameIds.All(id => endedSet.Contains(id));
      }
      return false;
   }

   public void ResetRoundEndTracking(string code)
   {
      if (_sessions.TryGetValue(code, out var session))
      {
         session.EndedGamesByRound[session.CurrentRound] = new HashSet<string>();
      }
   }
}

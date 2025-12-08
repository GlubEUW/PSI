using Api.Models;
using Api.GameLogic;

namespace Api.Services;

public class TournamentService(IGameFactory gameFactory, TournamentStore tournamentStore) : ITournamentService
{
   private readonly TournamentStore _store = tournamentStore;
   private readonly IGameFactory _gameFactory = gameFactory;
   public bool AddGameId(string code, Guid userId, string gameId = "")
   {
      if (string.IsNullOrEmpty(gameId))
         gameId = code;

      if (_store.Sessions.TryGetValue(code, out var session) && session is not null)
         return session.GamesByPlayers.TryAdd(userId, gameId);

      return false;
   }

   public bool RemoveGameId(string? code, Guid? userId)
   {
      if (code is null || userId is null)
         return false;

      if (_store.Sessions.TryGetValue(code, out var session) && session is not null)
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

      if (_store.Sessions.TryGetValue(code, out var session) && session is not null)
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
   public TournamentSession? GetTournamentSession(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
         return session;

      return null;
   }

   public RoundInfoDto GetTournamentRoundInfo(string code)
   {
      if (!_store.Sessions.TryGetValue(code, out var session) || session is null)
      {
         return new RoundInfoDto(1, 1);
      }

      var numberOfRounds = session.NumberOfRounds > 0 ? session.NumberOfRounds : 1;
      var currentRound = session.CurrentRound + 1;
      if (currentRound > numberOfRounds) currentRound = numberOfRounds;

      return new RoundInfoDto(currentRound, numberOfRounds);
   }

   public bool AreAllGamesEnded(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
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
}

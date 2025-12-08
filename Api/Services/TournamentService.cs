using Api.Models;
using Api.GameLogic;
using System.Collections.Concurrent;
using Api.Entities;

namespace Api.Services;

public class TournamentService(IGameService gameService, TournamentStore tournamentStore) : ITournamentService
{
   private readonly TournamentStore _store = tournamentStore;
   private readonly IGameService _gameService = gameService;

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
         var games = session.GamesByPlayers.Select(kv => kv.Value);
         return games.All(game => game.GameOver);
      }
      return false;
   }

   public ConcurrentDictionary<User, IGame> getGameListForCurrentRound(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
      {
         return session.GamesByPlayers;
      }
      return new ConcurrentDictionary<User, IGame>();
   }

   public string? StartNextRound(string code)
   {
      if (!_store.Sessions.TryGetValue(code, out var session) || session is null)
         return "Tournament session not found.";

      var players = session.Players;
      var (playerGroups, unmatchedPlayers) = CreateGroups(players, itemsPerGroup: 2);
      foreach (var group in playerGroups)
      {
         var game = _gameService.StartGame(session.GameTypesByRounds[session.CurrentRound], group);
         if (game is null)
         {
            session.GamesByPlayers.Clear();
            return "Failed to start game for a player group.";
         }
         foreach (var player in group)
         {
            session.GamesByPlayers[player] = game;
         }
      }

      session.CurrentRound++;
      session.RoundStarted = true;
      return null;
   }

   private (List<List<TItem>> groupedItems, List<TItem> ungroupedItems) CreateGroups<TItem>(
       List<TItem> items,
       int itemsPerGroup = 2,
       bool shuffle = true) where TItem : class, IComparable<TItem>
   {
      var groups = new List<List<TItem>>();
      var currentGroup = new List<TItem>();
      var count = 0;

      var processedItems = shuffle
          ? items.OrderBy(_ => Random.Shared.Next()).ToList()
          : items;

      foreach (var item in processedItems)
      {
         currentGroup.Add(item);
         count++;

         if (count == itemsPerGroup)
         {
            groups.Add(currentGroup);
            currentGroup = new List<TItem>();
            count = 0;
         }
      }

      var remaining = currentGroup.Count > 0 ? currentGroup : new List<TItem>();
      return (groups, remaining);
   }

   public bool HalfPlayersReadyForNextRound(string code)
   {
      return true;
   }

   public List<User>? getTargetGroup(User user, string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
      {
         var game = session.GamesByPlayers.FirstOrDefault(kv => kv.Key.Id == user.Id).Value;
         if (game is not null)
            return game.Players;
      }
      return null;
   }

   public bool RoundStarted(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
      {
         return session.RoundStarted;
      }
      return false;
   }

   public bool GetGame(string code, User user, out IGame? game)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
      {
         if (session.GamesByPlayers.TryGetValue(user, out var foundGame))
         {
            game = foundGame;
            return true;
         }
      }
      game = null;
      return false;
   }

   public bool TournamentStarted(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
      {
         return session.TournamentStarted;
      }
      return false;
   }

   public string? StartTournament(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
      {
         if (session.Players.Count < 2)
            return "Not enough players to start the tournament.";
         session.TournamentStarted = true;
         return null;
      }
      return "Tournament session not found.";
   }
}

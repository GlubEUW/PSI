using Api.Models;
using Api.GameLogic;
using System.Collections.Concurrent;
using Api.Entities;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TournamentService(
   IGameService gameService,
   TournamentStore tournamentStore,
   IDbContextFactory<DatabaseContext> contextFactory) : ITournamentService
{
   private readonly TournamentStore _store = tournamentStore;
   private readonly IGameService _gameService = gameService;
   private readonly IDbContextFactory<DatabaseContext> _contextFactory = contextFactory;

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
      var currentRound = session.CurrentRound > 0 ? session.CurrentRound : 1;
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

      if (session.CurrentRound >= session.NumberOfRounds)
      {
         return "All tournament rounds have been completed.";
      }

      if (session.Players.Count < 2)
      {
         return "Not enough players to start the next round.";
      }

      if (session.CurrentRound >= session.GameTypesByRounds.Count)
      {
         return $"No game type configured for round {session.CurrentRound + 1}.";
      }

      session.RoundStarted = false;
      session.GamesByPlayers.Clear();

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

   public async Task LogRoundStartAsync(string code)
   {
      if (!_store.Sessions.TryGetValue(code, out var session))
      {
         Console.WriteLine("Tournament session not found.");
         return;
      }

      Console.WriteLine($"Logging round start for tournament: {session.TournamentId}");
      Console.WriteLine($"CurrentRound: {session.CurrentRound}");
      Console.WriteLine($"GameTypesByRounds count: {session.GameTypesByRounds.Count}");

      try
      {
         await using var context = await _contextFactory.CreateDbContextAsync();

         var gameTypeIndex = session.CurrentRound - 1;
         Console.WriteLine($"Trying to access index: {gameTypeIndex}");

         if (gameTypeIndex < 0 || gameTypeIndex >= session.GameTypesByRounds.Count)
         {
            Console.WriteLine($"ERROR: Invalid game type index {gameTypeIndex} for list of size {session.GameTypesByRounds.Count}");
            return;
         }

         var uniqueGames = session.GamesByPlayers.Values.Distinct().ToList();
         Console.WriteLine($"Processing {uniqueGames.Count} unique games");

         foreach (var game in uniqueGames)
         {
            var gameEntity = new Game
            {
               Id = Guid.NewGuid(),
               TournamentId = session.TournamentId,
               GameType = session.GameTypesByRounds[gameTypeIndex].ToString(),
               RoundNumber = (short)session.CurrentRound
            };

            context.Games.Add(gameEntity);
            Console.WriteLine($"Added game to context: Round {gameEntity.RoundNumber}, GameType: {gameEntity.GameType}");

            var playersInGame = session.GamesByPlayers
               .Where(kv => kv.Value == game)
               .Select(kv => kv.Key)
               .ToList();

            for (var i = 0; i < playersInGame.Count; i++)
            {
               var userRound = new UserRound
               {
                  UserId = playersInGame[i].Id,
                  GameId = gameEntity.Id,
                  PlayerTurn = (short)(i + 1),
                  PlayerPlacement = 0
               };
               context.UserRound.Add(userRound);
            }
         }

         var changes = await context.SaveChangesAsync();
         Console.WriteLine($"Successfully saved {changes} changes to database");
      }
      catch (Exception ex)
      {
         Console.WriteLine($"ERROR in LogRoundStartAsync: {ex.Message}");
         Console.WriteLine($"Stack trace: {ex.StackTrace}");
         throw;
      }
   }

   public async Task SaveGameResultsAsync(string code)
   {
      Console.WriteLine($"[SaveGameResultsAsync] Called with code: {code}");

      if (!_store.Sessions.TryGetValue(code, out var session))
         return;

      try
      {
         await using var context = await _contextFactory.CreateDbContextAsync();

         var gamesInCurrentRound = await context.Games
            .Where(g => g.TournamentId == session.TournamentId && g.RoundNumber == session.CurrentRound)
            .ToListAsync();

         if (gamesInCurrentRound.Count == 0)
         {
            Console.WriteLine($"[SaveGameResultsAsync] No games found for round {session.CurrentRound}");
            return;
         }

         Console.WriteLine($"[SaveGameResultsAsync] Found {gamesInCurrentRound.Count} games in current round");

         var uniqueGames = session.GamesByPlayers.Values.Distinct().ToList();

         foreach (var memoryGame in uniqueGames)
         {
            if (!memoryGame.GameOver)
            {
               Console.WriteLine($"[SaveGameResultsAsync] Game not over, skipping");
               continue;
            }

            var winner = memoryGame.Winner;
            var playersInMemoryGame = memoryGame.Players;

            foreach (var dbGame in gamesInCurrentRound)
            {
               var userRoundsForGame = await context.UserRound
                  .Where(ur => ur.GameId == dbGame.Id)
                  .ToListAsync();

               var dbGamePlayerIds = userRoundsForGame.Select(ur => ur.UserId).ToHashSet();
               var memoryGamePlayerIds = playersInMemoryGame.Select(p => p.Id).ToHashSet();

               if (dbGamePlayerIds.SetEquals(memoryGamePlayerIds))
               {
                  Console.WriteLine($"[SaveGameResultsAsync] Found matching game in database");

                  foreach (var player in playersInMemoryGame)
                  {
                     var userRound = userRoundsForGame.FirstOrDefault(ur => ur.UserId == player.Id);

                     if (userRound != null && userRound.PlayerPlacement == 0)
                     {
                        if (winner == null)
                        {
                           userRound.PlayerPlacement = 1;
                        }
                        else
                        {
                           userRound.PlayerPlacement = (short)(player.Id == winner.Id ? 1 : 2);
                        }
                        context.UserRound.Update(userRound);
                        Console.WriteLine($"[SaveGameResultsAsync] Set placement for user {player.Id}: {userRound.PlayerPlacement}");
                     }
                  }
                  break;
               }
            }
         }

         var changes = await context.SaveChangesAsync();
         Console.WriteLine($"[SaveGameResultsAsync] Saved {changes} changes to database");
      }
      catch (Exception ex)
      {
         Console.WriteLine($"[SaveGameResultsAsync] ERROR: {ex.Message}");
         Console.WriteLine($"[SaveGameResultsAsync] Stack trace: {ex.StackTrace}");
         throw;
      }
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

   public async Task CheckAndSaveResultsIfAllGamesEndedAsync(string code)
   {
      if (RoundStarted(code) && AreAllGamesEnded(code))
      {
         Console.WriteLine($"All games ended for tournament {code}, saving results...");
         await SaveGameResultsAsync(code);
      }
   }

}
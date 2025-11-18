using System.Collections.Concurrent;
using System.Text.Json;

using Api.Entities;
using Api.GameLogic;

namespace Api.Services;

public class GameService(IGameFactory gameFactory) : IGameService
{
   private static readonly ConcurrentDictionary<string, IGame> _games = new();
   private readonly IGameFactory _gameFactory = gameFactory;
   public bool StartGame(string gameId, string gameType, List<User> players)
   {
      if (players == null || players.Count < 2)
         return false;
      if (_games.ContainsKey(gameId))
         return false;
      try
      {
         _games[gameId] = _gameFactory.CreateGame(gameType, players);
         Console.WriteLine($"Game {gameId} started with type {gameType}");
         return true;
      }
      catch (Exception ex)
      {
         Console.WriteLine($"Error starting game: {ex.Message}");
         _games.TryRemove(gameId, out _);
         return false;
      }
   }

   public bool RemoveGame(string gameId)
   {
      var removed = _games.TryRemove(gameId, out _);
      if (removed)
         Console.WriteLine($"Game {gameId} removed.");
      return removed;
   }

   public bool MakeMove(string gameId, JsonElement moveData, out object? newState)
   {
      newState = null;
      if (_games.TryGetValue(gameId, out var game) && game.MakeMove(moveData))
      {
         newState = game.GetState();
         return true;
      }
      return false;
   }

   public object? GetGameState(string gameId)
   {
      return _games.TryGetValue(gameId, out var game) ? game.GetState() : null;
   }

   public (List<List<TItem>> groupedItems, List<TItem> ungroupedItems) CreateGroups<TItem>(
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
}

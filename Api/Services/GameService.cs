using Api.Entities;
using Api.Enums;
using Api.GameLogic;

namespace Api.Services;

public class GameService(IGameFactory gameFactory) : IGameService
{
   private readonly IGameFactory _gameFactory = gameFactory;
   public IGame? StartGame(GameType gameType, List<User> players)
   {
      try
      {
         var game = _gameFactory.CreateGame(gameType, players);
         Console.WriteLine($"Game {game} started with type {gameType}");
         return game;
      }
      catch (Exception ex)
      {
         Console.WriteLine($"Error starting game: {ex.Message}");
         return null;
      }
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

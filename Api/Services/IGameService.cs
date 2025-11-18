using System.Text.Json;

using Api.Entities;

namespace Api.Services;

public interface IGameService
{
   public bool StartGame(string gameId, string gameType, List<User> players);
   public bool RemoveGame(string gameId);
   public bool MakeMove(string gameId, JsonElement data, out object? newState);
   public object? GetGameState(string gameId);
   public (List<List<TItem>> groupedItems, List<TItem> ungroupedItems) CreateGroups<TItem>(
    List<TItem> items,
    int itemsPerGroup = 2,
    bool shuffle = true) where TItem : class, IComparable<TItem>;
}

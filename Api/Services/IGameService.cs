using Api.Entities;
using Api.Enums;
using Api.GameLogic;

namespace Api.Services;

public interface IGameService
{
   public IGame? StartGame(GameType gameType, List<User> players);
   public (List<List<TItem>> groupedItems, List<TItem> ungroupedItems) CreateGroups<TItem>(
    List<TItem> items,
    int itemsPerGroup = 2,
    bool shuffle = true) where TItem : class, IComparable<TItem>;
}

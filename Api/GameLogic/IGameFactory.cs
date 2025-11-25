using Api.Entities;
using Api.Enums;

namespace Api.GameLogic;

public interface IGameFactory
{
   public IReadOnlySet<GameType> ValidGameTypes { get; }
   public IGame CreateGame(GameType gameType, List<User> players);
}

using Api.Entities;

namespace Api.GameLogic;

public interface IGameFactory
{
   public IReadOnlySet<string> ValidGameTypes { get; }
   public IGame CreateGame(string gameType, List<User> players);
}

using Api.Entities;
using Api.Enums;

namespace Api.GameLogic;

public class GameFactory : IGameFactory
{
   private readonly HashSet<GameType> _validGameTypes = new()
    {
        GameType.TicTacToe,
        GameType.RockPaperScissors,
        GameType.ConnectFour
    };

   public IReadOnlySet<GameType> ValidGameTypes => _validGameTypes;

   public IGame CreateGame(GameType gameType, List<User> players)
   {
      return gameType switch
      {
         GameType.TicTacToe => new TicTacToeGame(players),
         GameType.RockPaperScissors => new RockPaperScissorsGame(players),
         GameType.ConnectFour => new ConnectFourGame(players),
         _ => throw new ArgumentException($"Unknown game type: {gameType}")
      };
   }
}

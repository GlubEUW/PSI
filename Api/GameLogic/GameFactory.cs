using Api.Entities;

namespace Api.GameLogic;

public class GameFactory : IGameFactory
{
   private readonly HashSet<string> _validGameTypes = new()
    {
        "TicTacToe",
        "RockPaperScissors",
        "ConnectFour"
    };

   public IReadOnlySet<string> ValidGameTypes => _validGameTypes;

   public IGame CreateGame(string gameType, List<User> players)
   {
      return gameType switch
      {
         "TicTacToe" => new TicTacToeGame(players),
         "RockPaperScissors" => new RockPaperScissorsGame(players),
         "ConnectFour" => new ConnectFourGame(players),
         _ => throw new ArgumentException($"Unknown game type: {gameType}")
      };
   }
}

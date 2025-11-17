using Api.Entities;

namespace Api.GameLogic;

public static class GameFactory
{
   public static readonly HashSet<string> ValidGameTypes = new()
   {
      "TicTacToe",
      "RockPaperScissors",
      "ConnectFour"
   };
   public static IGame CreateGame(string gameType, List<User> players)
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

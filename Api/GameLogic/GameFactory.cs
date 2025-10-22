namespace Api.GameLogic;

public static class GameFactory
{
   public static IGame CreateGame(string gameType, List<Guid> playerIds, List<string> playerNames)
   {
      return gameType switch
      {
         "TicTacToe" => new TicTacToeGame(playerIds, playerNames),
         "RockPaperScissors" => new RpsGame(playerIds, playerNames),
         _ => throw new ArgumentException($"Unknown game type: {gameType}")
      };
   }
}
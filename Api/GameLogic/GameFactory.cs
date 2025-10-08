namespace Api.GameLogic;

public static class GameFactory
{
    public static IGame CreateGame(string gameType)
    {
        return gameType switch
        {
            "TicTacToe" => new TicTacToeGame(),
            "RockPaperScissors" => new RpsGame(),
            _ => throw new ArgumentException($"Unknown game type: {gameType}")
        };
    }
}
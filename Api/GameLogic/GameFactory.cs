namespace Api.GameLogic;

public static class GameFactory
{
    public static IGame CreateGame(string gameType, List<string> players)
    {
        return gameType switch
        {
            "TicTacToe" => new TicTacToeGame(players),
            "RockPaperScissors" => new RpsGame(players),
            _ => throw new ArgumentException($"Unknown game type: {gameType}")
        };
    }
}
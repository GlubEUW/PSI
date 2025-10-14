using Api.GameLogic;
namespace Api.Services;

public interface IGameService
{
    bool StartGame(string gameId, string gameType, List<string> players);
    bool RemoveGame(string gameId);
    bool MakeTicTacToeMove(string gameId, string playerName, int x, int y, out object? newState);
    bool MakeRpsMove(string gameId, string playerName, RpsChoice choice, out object? newState);
    object? GetGameState(string gameId);
}
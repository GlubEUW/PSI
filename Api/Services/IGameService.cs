using System.Text.Json;
namespace Api.Services;

public interface IGameService
{
   bool StartGame(string gameId, string gameType, List<string> players);
   bool RemoveGame(string gameId);
   bool MakeMove(string gameId, JsonElement data, out object? newState);
   object? GetGameState(string gameId);
}
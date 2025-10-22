using System.Text.Json;
using Api.Entities;

namespace Api.Services;

public interface IGameService
{
   bool StartGame(string gameId, string gameType, List<User> players);
   bool RemoveGame(string gameId);
   bool MakeMove(string gameId, JsonElement data, out object? newState);
   object? GetGameState(string gameId);
}
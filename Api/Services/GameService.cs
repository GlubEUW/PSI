using System.Collections.Concurrent;
using System.Text.Json;
using Api.Entities;
using Api.GameLogic;

namespace Api.Services;


public class GameService : IGameService
{
   private static readonly ConcurrentDictionary<string, IGame> _games = new();

   public bool StartGame(string gameId, string gameType, List<User> players)
   {
      if (players == null || players.Count < 2)
         return false;

      if (_games.ContainsKey(gameId))
         return false;

      try
      {
         _games[gameId] = GameFactory.CreateGame(gameType, players);
         Console.WriteLine($"Game {gameId} started with type {gameType}");
         return true;
      }
      catch (Exception ex)
      {
         Console.WriteLine($"Error starting game: {ex.Message}");
         return false;
      }
   }

   public bool RemoveGame(string gameId)
   {
      var removed = _games.TryRemove(gameId, out _);
      if (removed)
         Console.WriteLine($"Game {gameId} removed.");
      return removed;
   }

   public bool MakeMove(string gameId, JsonElement moveData, out object? newState)
   {
      newState = null;

      if (_games.TryGetValue(gameId, out var game) && game.MakeMove(moveData))
      {
         newState = game.GetState();
         return true;
      }

      return false;
   }

   public object? GetGameState(string gameId)
   {
      return _games.TryGetValue(gameId, out var game) ? game.GetState() : null;
   }
}

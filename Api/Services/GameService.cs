using Api.Entities;
using Api.Enums;
using Api.GameLogic;

namespace Api.Services;

public class GameService(IGameFactory gameFactory) : IGameService
{
   private readonly IGameFactory _gameFactory = gameFactory;
   public IGame? StartGame(GameType gameType, List<User> players)
   {
      if (players == null || players.Count < 2)
         return null;
      try
      {
         var game = _gameFactory.CreateGame(gameType, players);
         Console.WriteLine($"Game started with type {gameType}");
         return game;
      }
      catch (Exception ex)
      {
         Console.WriteLine($"Error starting game: {ex.Message}");
         return null;
      }
   }
}

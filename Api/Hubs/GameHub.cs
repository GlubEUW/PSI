using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Api.GameLogic;
using Api.Models;

namespace Api.Hubs;

public class GameHub : Hub
{
   private static ConcurrentDictionary<string, IGame> _games = new();

   public async Task StartGame(string gameId, string gameType)
   {
      if (!_games.ContainsKey(gameId))
      {
         _games[gameId] = GameFactory.CreateGame(gameType, MatchHub.GetPlayersForGame(gameId));
      }

      await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
      await Clients.Caller.SendAsync("GameUpdate", _games[gameId].GetState());
   }

   public static void RemoveGame(string gameId)
   {
      _games.TryRemove(gameId, out _);
      Console.WriteLine($"Game {gameId} removed.");
   }

   public async Task EndGame(string gameId)
   {
      RemoveGame(gameId);
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
   }

   public async Task MakeTicTacToeMove(string gameId, int x, int y, string playerName)
   {
      if (_games.TryGetValue(gameId, out var game))
      {
         var move = new TicTacToeMove { X = x, Y = y, PlayerName = playerName };

         if (game.MakeMove(playerName, move))
         {
            await Clients.Group(gameId).SendAsync("GameUpdate", game.GetState());
         }
      }
   }

   public async Task MakeRpsMove(string gameId, string playerName, RpsChoice choice)
   {
      if (_games.TryGetValue(gameId, out var game))
      {
         var move = new RpsMove { PlayerName = playerName, Choice = choice };

         if (game.MakeMove(playerName, move))
         {
            await Clients.Group(gameId).SendAsync("GameUpdate", game.GetState());
         }
      }
   }
}
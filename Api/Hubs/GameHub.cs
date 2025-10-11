using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Api.GameLogic;
using Api.Models;

namespace Api.Hubs;

public class GameHub : Hub
{
   private static ConcurrentDictionary<string, IGame> _games = new();

   public async Task StartGame(string gameID, string gameType)
   {
      if (!_games.ContainsKey(gameID))
      {
         _games[gameID] = GameFactory.CreateGame(gameType, MatchHub.GetPlayersForGame(gameID));
      }

      await Groups.AddToGroupAsync(Context.ConnectionId, gameID);
      await Clients.Caller.SendAsync("GameUpdate", _games[gameID].GetState());
   }

   public static void RemoveGame(string gameID)
   {
      _games.TryRemove(gameID, out _);
   }

   public async Task MakeTicTacToeMove(string gameID, int x, int y, string playerName)
   {
      if (_games.TryGetValue(gameID, out var game))
      {
         var move = new TicTacToeMove { X = x, Y = y, PlayerName = playerName };

         if (game.MakeMove(playerName, move))
         {
            await Clients.Group(gameID).SendAsync("GameUpdate", game.GetState());
         }
      }
   }

   public async Task MakeRpsMove(string gameID, string playerName, RpsChoice choice)
   {
      if (_games.TryGetValue(gameID, out var game))
      {
         var move = new RpsMove { PlayerName = playerName, Choice = choice };

         if (game.MakeMove("", move))
         {
            await Clients.Group(gameID).SendAsync("GameUpdate", game.GetState());
         }
      }
   }
}
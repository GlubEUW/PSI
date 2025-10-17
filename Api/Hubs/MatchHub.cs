using Microsoft.AspNetCore.SignalR;
using Api.Services;
using Api.GameLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Api.Hubs;

public class MatchHub : Hub
{
   private enum ContextKeys
   {
      PlayerName,
      Code
   }
   private readonly ILobbyService _lobbyService;
   private readonly IGameService _gameService;

   public MatchHub(ILobbyService lobbyService, IGameService gameService)
   {
      _lobbyService = lobbyService;
      _gameService = gameService;
   }

   public override async Task OnConnectedAsync()
   {
      var httpContext = Context.GetHttpContext();
      if (httpContext is null)
      {
         Context.Abort();
         return;
      }

      var code = httpContext.Request.Query["code"].ToString();
      var playerName = httpContext.Request.Query["playerName"].ToString();

      if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(playerName))
      {
         await Clients.Caller.SendAsync("Error", "Invalid connection parameters.");
         Context.Abort();
         return;
      }

      var joined = await _lobbyService.JoinMatch(code, playerName);
      if (!joined)
      {
         await Clients.Caller.SendAsync("Error", "Could not join the match.");
         Context.Abort();
         return;
      }

      Context.Items.Add(ContextKeys.Code, code);
      Context.Items.Add(ContextKeys.PlayerName, playerName);
      await Groups.AddToGroupAsync(Context.ConnectionId, code);
      await Clients.Group(code).SendAsync("PlayersUpdated", playerName);
      Console.WriteLine($"Player {playerName} connected to lobby {code}");
      await base.OnConnectedAsync();
   }

   public override async Task OnDisconnectedAsync(Exception? exception)
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new ArgumentNullException();
      var playerName = Context.Items[ContextKeys.PlayerName] as string ?? throw new ArgumentNullException();

      if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(playerName))
      {
         Console.WriteLine($"Player {playerName} disconnected from lobby {code}");
         await _lobbyService.LeaveMatch(code, playerName);
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
         await Clients.Group(code).SendAsync("PlayersUpdated", playerName);
      }
      else
      {
         Console.WriteLine("Could not retrieve lobby code or player name on disconnect.");
      }

      await base.OnDisconnectedAsync(exception);
   }

   public Task<List<string>> GetPlayers(string code)
   {
      return Task.FromResult(_lobbyService.GetPlayersInLobby(code));
   }

   public async Task StartMatch(string selectedGameType)
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new ArgumentNullException();
      var players = _lobbyService.GetPlayersInLobby(code);

      if (!_gameService.StartGame(code, selectedGameType, players))
      {
         await Clients.Caller.SendAsync("Error", "Failed to start the game.");
         return;
      }

      await Clients.Group(code).SendAsync("MatchStarted", new
      {
         gameType = selectedGameType,
         gameId = code,
         players,
         initialState = _gameService.GetGameState(code)
      });
   }

   public async Task MakeTicTacToeMove(string gameId, int x, int y, string playerName)
   {
      if (_gameService.MakeTicTacToeMove(gameId, playerName, x, y, out var newState))
      {
         await Clients.Group(gameId).SendAsync("GameUpdate", newState);
      }
   }

   public async Task MakeRpsMove(string gameId, string playerName, RpsChoice choice)
   {
      if (_gameService.MakeRpsMove(gameId, playerName, choice, out var newState))
      {
         await Clients.Group(gameId).SendAsync("GameUpdate", newState);
      }
   }

   public async Task EndGame(string gameId)
   {
      _gameService.RemoveGame(gameId);
      await Clients.Group(gameId).SendAsync("GameEnded");
   }

   public Task<object?> GetGameState(string gameId)
   {
      return Task.FromResult(_gameService.GetGameState(gameId));
   }
}
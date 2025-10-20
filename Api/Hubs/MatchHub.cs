using Microsoft.AspNetCore.SignalR;
using Api.Services;
using System.Text.Json;

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
      if (joined is not null)
      {
         await Clients.Caller.SendAsync("Error", "Could not join the match.");
         Context.Abort();
         return;
      }

      if(!_lobbyService.AddGameId(code, playerName)) // Later add gameId as argument to this 
      {
         await Clients.Caller.SendAsync("Error", "Could not add gameId.");
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
      var code = Context.Items[ContextKeys.Code] as string;
      var playerName = Context.Items[ContextKeys.PlayerName] as string;


      Console.WriteLine($"Player {playerName} disconnected from lobby {code}");

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

      if (!_lobbyService.RemoveGameId(code, playerName))
         Console.WriteLine($"Failed to remove gameId for player {playerName}");

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

   public async Task MakeMove(JsonElement moveData)
   {
      var code = Context.Items[ContextKeys.Code] as string;
      var playerName = Context.Items[ContextKeys.PlayerName] as string;
      if(_lobbyService.TryGetGameId(code, playerName, out var gameId) && gameId is not null)
      {
         if(_gameService.MakeMove(gameId, moveData, out var newState))
         {
            await Clients.Group(gameId).SendAsync("GameUpdate", newState);
         }
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
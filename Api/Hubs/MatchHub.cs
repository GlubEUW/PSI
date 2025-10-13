using Microsoft.AspNetCore.SignalR;
using Api.Models;
using Api.Services;

namespace Api.Hubs;

public class MatchHub : Hub
{
   private readonly ILobbyService _lobbyService;

   public MatchHub(ILobbyService lobbyService)
   {
      _lobbyService = lobbyService;
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

      var joined = _lobbyService.JoinMatch(code, playerName);
      if (!joined)
      {
         await Clients.Caller.SendAsync("Error", "Could not join the match.");
         Context.Abort();
         return;
      }

      await Groups.AddToGroupAsync(Context.ConnectionId, code);
      await Clients.Group(code).SendAsync("PlayersUpdated", playerName);
      Console.WriteLine($"Player {playerName} connected to lobby {code}");
      await base.OnConnectedAsync();
   }

   public override async Task OnDisconnectedAsync(Exception? exception)
   {
      var httpContext = Context.GetHttpContext();
      if (httpContext is null)
      {
         await base.OnDisconnectedAsync(exception);
         return;
      }

      var code = httpContext.Request.Query["code"].ToString();
      var playerName = httpContext.Request.Query["playerName"].ToString();

      Console.WriteLine($"Player {playerName} disconnected from lobby {code}");

      if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(playerName))
      {
         _lobbyService.LeaveMatch(code, playerName);
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
         await Clients.Group(code).SendAsync("PlayersUpdated", playerName);
      }

      await base.OnDisconnectedAsync(exception);
   }
   public List<string> GetPlayers(string code)
   {
      return _lobbyService.GetPlayersInLobby(code);
   }
   public async Task StartMatch(string selectedGameType, string code)
   {
      //_lobbyService.StartMatch(code, selectedGameType);
   }
}

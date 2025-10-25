using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Security.Claims;
using Api.Services;
using Api.Entities;
using Api.Models;
using Api.Enums;

namespace Api.Hubs;

public class MatchHub(ILobbyService lobbyService, IGameService gameService) : Hub
{
   private enum ContextKeys
   {
      User,
      Code
   }
   private readonly ILobbyService _lobbyService = lobbyService;
   private readonly IGameService _gameService = gameService;

   public override async Task OnConnectedAsync()
   {
      var httpContext = Context.GetHttpContext();
      if (httpContext is null)
      {
         Context.Abort();
         return;
      }

      var code = httpContext.Request.Query["code"].ToString();
      if (string.IsNullOrEmpty(code))
      {
         await Clients.Caller.SendAsync("Error", "Invalid connection parameters.");
         Context.Abort();
         return;
      }

      var userName = Context.User?.Identity?.Name;
      var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

      if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
      {
         await Clients.Caller.SendAsync("Error", "User not authenticated.");
         Context.Abort();
         return;
      }

      var user = new User
      {
         Id = Guid.Parse(userId),
         Name = userName,
         Role = Enum.Parse<UserRole>(userRole)
      };

      var joined = await _lobbyService.JoinMatch(code, user);
      if (joined is not null)
      {
         await Clients.Caller.SendAsync("Error", "Could not join the match.");
         Context.Abort();
         return;
      }

      if (!_lobbyService.AddGameId(code: code, userId: user.Id))
      {
         await Clients.Caller.SendAsync("Error", "Could not add gameId.");
         Context.Abort();
         return;
      }


      Context.Items.Add(ContextKeys.Code, code);
      Context.Items.Add(ContextKeys.User, user);
      await Groups.AddToGroupAsync(Context.ConnectionId, code);

      var roundInfo = _lobbyService.GetMatchRoundInfo(code);
      await Clients.Group(code).SendAsync("PlayersUpdated", roundInfo);
      Console.WriteLine($"Player {user.Name} connected to lobby {code}");
      await base.OnConnectedAsync();
   }

   public override async Task OnDisconnectedAsync(Exception? exception)
   {
      var code = Context.Items[ContextKeys.Code] as string;
      var user = Context.Items[ContextKeys.User] as User;

      if (!string.IsNullOrEmpty(code) && user is not null)
      {

         Console.WriteLine($"Player {user.Name} disconnected from lobby {code}");
         await _lobbyService.LeaveMatch(code, user.Id);
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);

         var roundInfo = _lobbyService.GetMatchRoundInfo(code);
         await Clients.Group(code).SendAsync("PlayersUpdated", roundInfo);
      }
      else
      {
         Console.WriteLine("Could not retrieve lobby code or player name on disconnect.");
      }

      if (user is not null && !_lobbyService.RemoveGameId(code, user.Id))
         Console.WriteLine($"Failed to remove gameId for player {user.Name}");

      await base.OnDisconnectedAsync(exception);
   }

   public Task<List<PlayerInfoDto>> GetPlayers(string code)
   {
      var players = _lobbyService.GetPlayersInLobby(code);
      var playerNames = players.Select(p => p.Name).ToList();
      var playerDtos = players.Select(p => new PlayerInfoDto(p.Name, p.Wins)).ToList();
      return Task.FromResult(playerDtos);
   }

   public async Task StartMatch()
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new ArgumentNullException();
      var session = _lobbyService.GetMatchSession(code) ?? throw new ArgumentNullException();

      if (session.CurrentRound >= session.GamesList.Count)
      {
         await Clients.Group(code).SendAsync("RoundsEnded");
         return;
      }

      var selectedGameType = session.GamesList[session.CurrentRound];
      session.GameType = selectedGameType;
      session.InGame = true;
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
         playerIds = players.Select(p => p.Id).ToList(),
         initialState = _gameService.GetGameState(code),
         round = session.CurrentRound + 1
      });

      session.CurrentRound++;
   }


   public async Task MakeMove(JsonElement moveData)
   {
      var code = Context.Items[ContextKeys.Code] as string;
      var user = Context.Items[ContextKeys.User] as User;
      if (user is not null && _lobbyService.TryGetGameId(code, user.Id, out var gameId) && gameId is not null)
      {
         if (_gameService.MakeMove(gameId, moveData, out var newState))
            await Clients.Group(gameId).SendAsync("GameUpdate", newState);
      }
   }

   public async Task EndGame(string gameId)
   {
      var code = Context.Items[ContextKeys.Code] as string;
      if (!string.IsNullOrEmpty(code))
      {
         var roundInfo = _lobbyService.GetMatchRoundInfo(code);
         await Clients.Group(code).SendAsync("PlayersUpdated", roundInfo);
      }
      _gameService.RemoveGame(gameId);
   }

   public Task<object?> GetGameState(string gameId)
   {
      return Task.FromResult(_gameService.GetGameState(gameId));
   }
}

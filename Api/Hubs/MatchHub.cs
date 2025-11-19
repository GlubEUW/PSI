using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Security.Claims;
using Api.Services;
using Api.Entities;
using Api.Models;

namespace Api.Hubs;

public class MatchHub(ILobbyService lobbyService, IGameService gameService, IUserService userService) : Hub
{
   private enum ContextKeys
   {
      User,
      Code
   }
   private readonly ILobbyService _lobbyService = lobbyService;
   private readonly IGameService _gameService = gameService;
   private readonly IUserService _userService = userService;

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

      User user = _userService.CreateUser(userName, Guid.Parse(userId), userRole);

      var joined = await _lobbyService.JoinMatch(code, user);
      if (joined is not null)
      {
         await Clients.Caller.SendAsync("Error", "Could not join the match.");
         Context.Abort();
         return;
      }

      await _userService.LoadUserStatsAsync(user);

      Context.Items.Add(ContextKeys.Code, code);
      Context.Items.Add(ContextKeys.User, user);
      await Groups.AddToGroupAsync(Context.ConnectionId, code);
      await Groups.AddToGroupAsync(Context.ConnectionId, user.Id.ToString());

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

         await _userService.SaveUserStatsAsync(user);

         await _lobbyService.LeaveMatch(code, user.Id);

         await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.Id.ToString());

         var roundInfo = _lobbyService.GetMatchRoundInfo(code);
         await Clients.Group(code).SendAsync("PlayersUpdated", roundInfo);
      }
      else
      {
         Console.WriteLine("Could not retrieve lobby code or player name on disconnect.");
      }

   }

   public Task<List<PlayerInfoDto>> GetPlayers(string code)
   {
      var players = _lobbyService.GetPlayersInLobby(code);
      var playerDtos = players.Select(p => new PlayerInfoDto(p.Name, p.Wins)).ToList();
      return Task.FromResult(playerDtos);
   }

   public async Task StartMatch()
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new ArgumentNullException();
      var session = _lobbyService.GetMatchSession(code) ?? throw new ArgumentNullException();

      if (!_lobbyService.AreAllPlayersInLobby(code))
      {
         await Clients.Caller.SendAsync("Error", "Not all players have returned to the lobby yet.");
         return;
      }

      if (session.InGame)
      {
         await Clients.Caller.SendAsync("Error", "A game is still in progress.");
         return;
      }

      if (session.CurrentRound >= session.GamesList.Count)
      {
         await Clients.Group(code).SendAsync("RoundsEnded");
         return;
      }

      var selectedGameType = session.GamesList[session.CurrentRound];
      var players = _lobbyService.GetPlayersInLobby(code);
      var (playerGroups, unmatchedPlayers) = _gameService.CreatePlayerGroups(players, playersPerGame: 2);

      foreach (var unmatchedPlayer in unmatchedPlayers)
      {
         await Clients.Group(unmatchedPlayer.Id.ToString()).SendAsync("NoPairing");
      }

      var gameInfos = new List<(string gameId, List<User> group)>();
      var i = 0;
      foreach (var group in playerGroups)
      {
         var gameId = $"{code}_G{i}_R{session.CurrentRound}";
         if (!_gameService.StartGame(gameId, selectedGameType, group))
         {
            foreach (var (createdGameId, _) in gameInfos)
            {
               _gameService.RemoveGame(createdGameId);
            }
            await Clients.Caller.SendAsync("Error", "Failed to start the game.");
            return;
         }
         gameInfos.Add((gameId, group));
         i++;
      }

      session.GameType = selectedGameType;
      session.PlayerGroups = playerGroups;
      session.InGame = true;
      _lobbyService.ResetRoundEndTracking(code);

      i = 0;
      foreach (var (gameId, group) in gameInfos)
      {
         foreach (var player in group)
         {
            _lobbyService.AddGameId(code, player.Id, gameId);
            await Clients.Group(player.Id.ToString()).SendAsync("MatchStarted", new
            {
               gameType = selectedGameType,
               gameId,
               playerIds = group.Select(p => p.Id),
               initialState = _gameService.GetGameState(gameId),
               round = session.CurrentRound + 1
            });
         }
         i++;

      }
      session.CurrentRound++;
   }

   public async Task MakeMove(JsonElement moveData)
   {
      var code = Context.Items[ContextKeys.Code] as string
          ?? throw new InvalidOperationException("Code not found in context");

      var user = Context.Items[ContextKeys.User] as User
          ?? throw new InvalidOperationException("User not found in context");

      var session = _lobbyService.GetMatchSession(code)
          ?? throw new InvalidOperationException($"Match session not found for code: {code}");

      if (!_lobbyService.TryGetGameId(code, user.Id, out var gameId) || gameId is null)
      {
         await Clients.Caller.SendAsync("Error", "You are not currently in an active game.");
         return;
      }

      if (!_gameService.MakeMove(gameId, moveData, out var newState))
         return;

      var targetGroup = session.PlayerGroups.FirstOrDefault(g => g.Any(p => p.Id == user.Id));
      if (targetGroup is null)
      {
         await Clients.Caller.SendAsync("Error", "Your player group could not be found.");
         return;
      }

      var notifyTasks = targetGroup.Select(p =>
          Clients.Group(p.Id.ToString()).SendAsync("GameUpdate", newState)
      );

      await Task.WhenAll(notifyTasks);
   }



   public async Task EndGame(string gameId)
   {
      var code = Context.Items[ContextKeys.Code] as string
          ?? throw new InvalidOperationException("Match code not found in context");

      _gameService.RemoveGame(gameId);
      _lobbyService.MarkGameAsEnded(code, gameId);

      if (_lobbyService.AreAllGamesEnded(code))
      {
         var roundInfo = _lobbyService.GetMatchRoundInfo(code);
         await Clients.Group(code).SendAsync("PlayersUpdated", roundInfo);
         await Clients.Group(code).SendAsync("RoundEnded", new { roundInfo });
      }
      else
      {
         await Clients.Group(gameId).SendAsync("GameEnded", new { gameId });
      }
   }


   public Task<object?> GetGameState(string gameId)
   {
      return Task.FromResult(_gameService.GetGameState(gameId));
   }
}

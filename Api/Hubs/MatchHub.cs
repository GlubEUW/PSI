using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Security.Claims;
using Api.Services;
using Api.Entities;
using Api.Models;

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
   private static readonly Dictionary<string, HashSet<Guid>> _lobbyScreenUsers = new();

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

      User user = UserService.CreateUser(userName, Guid.Parse(userId), userRole);

      var joined = await _lobbyService.JoinMatch(code, user);
      if (joined is not null)
      {
         await Clients.Caller.SendAsync("Error", "Could not join the match.");
         Context.Abort();
         return;
      }

      Context.Items.Add(ContextKeys.Code, code);
      Context.Items.Add(ContextKeys.User, user);
      await Groups.AddToGroupAsync(Context.ConnectionId, code);
      await Groups.AddToGroupAsync(Context.ConnectionId, user.Id.ToString());

      if (!_lobbyScreenUsers.ContainsKey(code))
      {
         _lobbyScreenUsers[code] = new HashSet<Guid>();
      }
      _lobbyScreenUsers[code].Add(user.Id);

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

         if (_lobbyScreenUsers.ContainsKey(code))
         {
            _lobbyScreenUsers[code].Remove(user.Id);
            if (_lobbyScreenUsers[code].Count == 0)
            {
               _lobbyScreenUsers.Remove(code);
            }
         }

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

   public Task UpdateLobbyStatus(bool isInLobby)
   {
      var code = Context.Items[ContextKeys.Code] as string;
      var user = Context.Items[ContextKeys.User] as User;

      if (string.IsNullOrEmpty(code) || user is null)
         return Task.CompletedTask;

      if (!_lobbyScreenUsers.ContainsKey(code))
      {
         _lobbyScreenUsers[code] = new HashSet<Guid>();
      }

      if (isInLobby)
      {
         _lobbyScreenUsers[code].Add(user.Id);
      }
      else
      {
         _lobbyScreenUsers[code].Remove(user.Id);
      }

      return Task.CompletedTask;
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

      var players = _lobbyService.GetPlayersInLobby(code);

      if (!_lobbyScreenUsers.ContainsKey(code))
      {
         await Clients.Caller.SendAsync("CannotStartMatch", "No players are in the lobby screen.");
         return;
      }

      var playersInLobby = _lobbyScreenUsers[code];
      var allPlayersInLobby = players.All(p => playersInLobby.Contains(p.Id));

      if (!allPlayersInLobby)
      {
         await Clients.Caller.SendAsync("CannotStartMatch", "All players must be in the lobby screen to start the match.");
         return;
      }

      var selectedGameType = session.GamesList[session.CurrentRound];
      session.GameType = selectedGameType;
      session.InGame = true;

      var playersPerGame = 2;
      var pair = 0;
      var playerGroups = new List<List<User>>();
      var currentGroup = new List<User>();

      var random = new Random();
      var shuffledPlayers = players.OrderBy(_ => random.Next()).ToList();

      foreach (var player in shuffledPlayers)
      {
         currentGroup.Add(player);
         pair++;

         if (pair == playersPerGame)
         {
            playerGroups.Add(currentGroup);
            currentGroup = new List<User>();
            pair = 0;
         }
      }
      if (currentGroup.Count > 0)
      {
         foreach (var unmatchedPlayer in currentGroup)
         {
            await Clients.Group(unmatchedPlayer.Id.ToString()).SendAsync("NoPairing");
         }
      }

      session.PlayerGroups = playerGroups;
      _lobbyService.ResetRoundEndTracking(code);
      var i = 0;
      foreach (var group in playerGroups)
      {
         var gameId = $"{code}_G{i}_R{session.CurrentRound}";
         if (!_gameService.StartGame(gameId, selectedGameType, group))
         {
            await Clients.Caller.SendAsync("Error", "Failed to start the game.");
            return;
         }

         foreach (var player in group)
         {
            _lobbyService.AddGameId(code, player.Id, gameId);
            await Clients.Group(player.Id.ToString()).SendAsync("MatchStarted", new
            {
               gameType = selectedGameType,
               gameId = $"{code}_G{i}_R{session.CurrentRound}",
               playerIds = group.Select(p => p.Id),
               initialState = _gameService.GetGameState($"{code}_G{i}_R{session.CurrentRound}"),
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

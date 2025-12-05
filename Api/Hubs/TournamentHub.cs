using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Api.Entities;
using Api.Models;
using Api.Exceptions;
using Api.Utils;
using Api.Services;

namespace Api.Hubs;

public class TournamentHub(ITournamentService tournamentService, ILobbyService lobbyService, IGameService gameService, IUserService userService, ICurrentUserAccessor currentUserAccessor) : Hub
{
   private enum ContextKeys
   {
      User,
      Code
   }
   private readonly ILobbyService _lobbyService = lobbyService;
   private readonly ITournamentService _tournamentService = tournamentService;
   private readonly IGameService _gameService = gameService;
   private readonly IUserService _userService = userService;
   private readonly ICurrentUserAccessor _currentUserAccessor = currentUserAccessor;

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

      var user = _currentUserAccessor.GetCurrentUser(Context); ;
      if (user is null)
      {
         await Clients.Caller.SendAsync("Error", "User not authenticated.");
         Context.Abort();
         return;
      }

      // This is not finished we also have to consider the cases where the user is already in a match
      // and we want to log them back in to the same match
      // Two things coud be happening here:
      // 1. Someone is joining a tournament lobby as that user, in which case do not kick him out maybe ping him?
      // 2. User is gone and we need to let him rejoin
      var joined = await _lobbyService.JoinLobby(code, user);
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

      var roundInfo = _tournamentService.GetTournamentRoundInfo(code);
      await Clients.Group(code).SendAsync("PlayersUpdated", roundInfo);
      await base.OnConnectedAsync();
   }

   public override async Task OnDisconnectedAsync(Exception? exception)
   {
      var code = Context.Items[ContextKeys.Code] as string;
      var user = Context.Items[ContextKeys.User] as User;

      if (!string.IsNullOrEmpty(code) && user is not null)
      {
         // await _tournamentService.LeaveTournament(code, user.Id);

         await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.Id.ToString());

         var roundInfo = _tournamentService.GetTournamentRoundInfo(code);
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

   public async Task StartTournament()
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new ArgumentNullException();
      var session = _tournamentService.GetTournamentSession(code) ?? throw new ArgumentNullException();

      if (session.TournamentStarted)
      {
         await Clients.Caller.SendAsync("Error", "The tournament has already started.");
         throw new InvalidOperationException("Tournament already started.");
      }
   }

   public async Task StartRound()
   {
      var code = Context.Items[ContextKeys.Code] as string
          ?? throw new InvalidOperationException("Match code not found in context");

      if (_tournamentService.RoundStarted(code))
      {
         await Clients.Caller.SendAsync("Error", "Round has already been started.");
         throw new InvalidOperationException("Round has already been started for tournament with code: " + code);
      }

      if (!_tournamentService.AreAllGamesEnded(code))
      {
         await Clients.Caller.SendAsync("Error", "Not all games have ended.");
      }

      // while (_tournamentService.HalfPlayersReadyForNextRound(code) && !_tournamentService.AllPlayersReadyForNextRound(code))
      // {
      //    Clients.Caller.SendAsync("WaitingForPlayers", _tournamentService.getReadyPlayerCount(code).ToString());
      // } not good enough need to think about the case where no players ready anymore

      if (_tournamentService.StartNextRound(code) is not null)
      {
         await Clients.Caller.SendAsync("Error", "Could not start the next round.");
      }

      foreach (var game in _tournamentService.getGameListForCurrentRound(code).Values)
      {
         foreach (var player in game.Players)
         {
            await Clients.Group(player.Id.ToString()).SendAsync("GameStarted", new
            {
               gameType = game.GameType
            });
         }
      }
   }

   public async Task MakeMove(JsonElement moveData)
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new InvalidOperationException("Code not found in context");

      var user = Context.Items[ContextKeys.User] as User ?? throw new InvalidOperationException("User not found in context");

      try
      {
         if (!_tournamentService.TryGetGameId(code, user.Id, out var gameId) || gameId is null)
         {
            throw new GameNotFoundException(gameId ?? "unknown");
         }

         if (!_gameService.MakeMove(gameId, moveData, out var newState))
         {
            return;
         }

         var targetGroup = _tournamentService.getTargetGroup(user, code) ?? throw new PlayerNotFoundException(user.Id, code);

         var notifyTasks = targetGroup.Select(p =>
             Clients.Group(p.Id.ToString()).SendAsync("GameUpdate", newState)
         );

         await Task.WhenAll(notifyTasks);
      }
      catch (InvalidMoveException ex)
      {
         ExceptionLogger.LogException(ex, "MakeMove - Invalid move attempted");
         await Clients.Caller.SendAsync("Error", ex.Message);
      }
      catch (Exception ex)
      {
         ExceptionLogger.LogException(ex, "MakeMove - Unexpected error");
         await Clients.Caller.SendAsync("Error", "An unexpected error occurred");
      }
   }

   public Task<object?> GetGameState(string gameId)
   {
      return Task.FromResult(_gameService.GetGameState(gameId));
   }
}

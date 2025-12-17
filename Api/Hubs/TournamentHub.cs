using Microsoft.AspNetCore.SignalR;

using System.Text.Json;

using Api.Entities;
using Api.Exceptions;
using Api.Utils;
using Api.Services;
using Api.Models;

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

      var user = _currentUserAccessor.GetCurrentUser(Context);
      if (user is null)
      {
         await Clients.Caller.SendAsync("Error", "User not authenticated.");
         Context.Abort();
         return;
      }

      var tournamentSession = _tournamentService.GetTournamentSession(code);
      if (tournamentSession is null)
      {
         await Clients.Caller.SendAsync("Error", "Tournament not found.");
         Context.Abort();
         return;
      }

      if (_tournamentService.TournamentStarted(code))
      {
         if (tournamentSession.Players.Contains(user))
         {
            Context.Items.Add(ContextKeys.Code, code);
            Context.Items.Add(ContextKeys.User, user);
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            await Groups.AddToGroupAsync(Context.ConnectionId, user.Id.ToString());
         }
         else
         {
            await Clients.Caller.SendAsync("Error", "Tournament has already started.");
            Context.Abort();
            return;
         }
      }
      else
      {
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

         await Clients.Group(code).SendAsync("PlayersUpdated", _tournamentService.GetTournamentRoundInfo(code));
      }
      await base.OnConnectedAsync();
   }

   public override async Task OnDisconnectedAsync(Exception? exception)
   {
      var code = Context.Items[ContextKeys.Code] as string;
      var user = Context.Items[ContextKeys.User] as User;

      if (!string.IsNullOrEmpty(code) && user is not null)
      {
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.Id.ToString());
         if (!_tournamentService.TournamentStarted(code))
         {
            await _lobbyService.LeaveLobby(code, user.Id);
            await Clients.Group(code).SendAsync("PlayersUpdated", _tournamentService.GetTournamentRoundInfo(code));
         }
      }
      else
      {
         Console.WriteLine("Could not retrieve lobby code or player name on disconnect.");
      }
      await base.OnDisconnectedAsync(exception);
   }

   public Task<List<PlayerInfoDto>> GetPlayers(string code)
   {
      return _lobbyService.GetPlayersInLobbyDTOs(code);
   }

   public async Task StartTournament()
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new ArgumentNullException();
      var session = _tournamentService.GetTournamentSession(code) ?? throw new ArgumentNullException();

      if (session.TournamentStarted)
      {
         await Clients.Caller.SendAsync("Error", "The tournament has already started.");
         return;
      }

      var startResult = _tournamentService.StartTournament(code);
      if (startResult is not null)
      {
         await Clients.Caller.SendAsync("Error", startResult);
         return;
      }
   }

   public async Task StartRound()
   {
      var code = Context.Items[ContextKeys.Code] as string
          ?? throw new InvalidOperationException("Match code not found in context");

      if (!_tournamentService.TournamentStarted(code))
      {
         await Clients.Caller.SendAsync("Error", "Tournament has not started yet.");
         return;
      }

      if (_tournamentService.RoundStarted(code) && !_tournamentService.AreAllGamesEnded(code))
      {
         await Clients.Caller.SendAsync("Error", "Round is still in progress. Wait for all games to finish.");
         return;
      }

      var roundStartError = _tournamentService.StartNextRound(code);
      if (roundStartError is not null)
      {
         await Clients.Caller.SendAsync("Error", roundStartError);
         return;
      }

      await _tournamentService.LogRoundStartAsync(code);
      Console.WriteLine("Logged new round start");

      await Clients.Group(code).SendAsync("PlayersUpdated", _tournamentService.GetTournamentRoundInfo(code));

      foreach (var game in _tournamentService.getGameListForCurrentRound(code).Values)
      {
         foreach (var player in game.Players)
         {
            await Clients.Group(player.Id.ToString()).SendAsync("GameStarted", new
            {
               gameType = game.GameType.ToString()
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
         if (!_tournamentService.GetGame(code, user, out var game) || game is null)
         {
            throw new GameNotFoundException("unknown");
         }

         game.MakeMove(moveData, user);

         var targetGroup = game.Players;
         var notifyTasks = targetGroup.Select(p =>
             Clients.Group(p.Id.ToString()).SendAsync("GameUpdate", game.GetState())
         );

         await Task.WhenAll(notifyTasks);

         await _tournamentService.CheckAndSaveResultsIfAllGamesEndedAsync(code);
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

   public Task<object?> GetGameState()
   {
      var code = Context.Items[ContextKeys.Code] as string ?? throw new InvalidOperationException("Code not found in context");
      var user = Context.Items[ContextKeys.User] as User ?? throw new InvalidOperationException("User not found in context");
      _tournamentService.GetGame(code, user, out var game);
      return Task.FromResult(game?.GetState());
   }
}
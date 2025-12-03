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
      Session
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

      var session = _tournamentService.GetTournamentSession(code) ?? throw new InvalidOperationException($"Match session not found for code: {code}");
      Context.Items.Add(ContextKeys.Session, session);
      Context.Items.Add(ContextKeys.User, user);
      await Groups.AddToGroupAsync(Context.ConnectionId, session.Code);
      await Groups.AddToGroupAsync(Context.ConnectionId, user.Id.ToString());

      var roundInfo = _tournamentService.GetTournamentRoundInfo(session.Code);
      await Clients.Group(session.Code).SendAsync("PlayersUpdated", roundInfo);
      await base.OnConnectedAsync();
   }

   public override async Task OnDisconnectedAsync(Exception? exception)
   {
      var session = Context.Items[ContextKeys.Session] as TournamentSession;
      var user = Context.Items[ContextKeys.User] as User;

      if (session is not null && user is not null)
      {
         // await _tournamentService.LeaveTournament(code, user.Id);

         await Groups.RemoveFromGroupAsync(Context.ConnectionId, session.Code);
         await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.Id.ToString());

         var roundInfo = _tournamentService.GetTournamentRoundInfo(session.Code);
         await Clients.Group(session.Code).SendAsync("PlayersUpdated", roundInfo);
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
      var session = Context.Items[ContextKeys.Session] as TournamentSession ?? throw new ArgumentNullException();

      if (session.TournamentStarted)
      {
         await Clients.Caller.SendAsync("Error", "The tournament has already started.");
         throw new InvalidOperationException("Tournament already started.");
      }
   }

   public async Task StartRound()
   {
      var session = Context.Items[ContextKeys.Session] as TournamentSession ?? throw new ArgumentNullException();

      // IMPLEMENT READY AND IF AT LEAST HALF OF THEM ARE READY START COUNTDOWN OF 5 SECONDS
      // if (!_lobbyService.AreAllPlayersInLobby(code))
      // {
      //    await Clients.Caller.SendAsync("Error", "Not all players have returned to the lobby yet.");
      //    return;
      // }

      if (session.CurrentRound >= session.GameTypesByRounds.Count)
      {
         await Clients.Group(session.Code).SendAsync("RoundsEnded");
         return;
      }

      var selectedGameType = session.GameTypesByRounds[session.CurrentRound];
      var players = session.Players;
      var (playerGroups, unmatchedPlayers) = _gameService.CreateGroups<User>(players, itemsPerGroup: 2); //WHYYYYYYY just add the static variable per iGAME

      foreach (var unmatchedPlayer in unmatchedPlayers)
      {
         await Clients.Group(unmatchedPlayer.Id.ToString()).SendAsync("NoPairing");
      }

      foreach (var group in playerGroups)
      {
         var game = _gameService.StartGame(selectedGameType, group);
         if (game is not null)
         {
            foreach (var player in group)
            {
               session.GamesByPlayers[player] = game;
            }
         }
         else
         {
            throw new GameException("Failed to start game.");
         }
      }

      session.TournamentStarted = true;

      foreach (var player in players)
      {
         var game = session.GamesByPlayers[player];
         await Clients.Group(player.Id.ToString()).SendAsync("MatchStarted", new
         {
            gameType = selectedGameType,
            playerIds = session.GamesByPlayers.Keys.Where(u => session.GamesByPlayers[u] == game).Select(u => u.Id).ToList(),
            initialState = game.GetState(),
            round = ++session.CurrentRound
         });
      }
   }

   public async Task MakeMove(JsonElement moveData)
   {
      try
      {
         var session = Context.Items[ContextKeys.Session] as TournamentSession
               ?? throw new InvalidOperationException("Session not found in context");

         var user = Context.Items[ContextKeys.User] as User
               ?? throw new InvalidOperationException("User not found in context");

         var game = session.GamesByPlayers[user];
         if (!game.MakeMove(moveData))
         {
            return;
         }

         var targetGroup = session.GamesByPlayers[user].Players ?? throw new InvalidOperationException("Players not found in game");

         var notifyTasks = targetGroup.Select(p =>
             Clients.Group(p.Id.ToString()).SendAsync("GameUpdate", game.GetState())
         );

         await Task.WhenAll(notifyTasks);
      }
      catch (InvalidMoveException ex)
      {
         ExceptionLogger.LogException(ex, "MakeMove - Invalid move attempted");
         await Clients.Caller.SendAsync("Error", ex.Message);
      }
      catch (GameNotFoundException ex)
      {
         ExceptionLogger.LogException(ex, "MakeMove - Game not found");
         await Clients.Caller.SendAsync("Error", "Game session not found");
      }
      catch (PlayerNotFoundException ex)
      {
         ExceptionLogger.LogException(ex, "MakeMove - Player not found");
         await Clients.Caller.SendAsync("Error", "Player not found in game");
      }
      catch (Exception ex)
      {
         ExceptionLogger.LogException(ex, "MakeMove - Unexpected error");
         await Clients.Caller.SendAsync("Error", "An unexpected error occurred");
      }
   }

   public Task<object> GetGameState()
   {
      var session = Context.Items[ContextKeys.Session] as TournamentSession
            ?? throw new InvalidOperationException("Session not found in context");
      var user = Context.Items[ContextKeys.User] as User
            ?? throw new InvalidOperationException("User not found in context");
      var game = session.GamesByPlayers[user]
            ?? throw new GameNotFoundException("Game not found for user in session");
      return Task.FromResult(game.GetState());
   }
}

using Microsoft.AspNetCore.SignalR;
using Api.Services;
using Api.Entities;
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
   private readonly ITournamentService _tournamentService;

   public MatchHub(ILobbyService lobbyService, IGameService gameService, ITournamentService tournamentService)
   {
      _lobbyService = lobbyService;
      _gameService = gameService;
      _tournamentService = tournamentService;
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

      if (!_lobbyService.AddGameId(code, playerName))
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

   public async Task StartTournament(string tournamentId, string gameType)
   {
      var players = _lobbyService.GetPlayersInLobby(tournamentId);

      if (players.Count < 2)
      {
         await Clients.Caller.SendAsync("Error", "Need at least 2 players to start tournament.");
         return;
      }

      if (!_tournamentService.CreateTournament(tournamentId, players, gameType))
      {
         await Clients.Caller.SendAsync("Error", "Failed to create tournament.");
         return;
      }

      var status = _tournamentService.GetTournamentStatus(tournamentId);
      await Clients.Group(tournamentId).SendAsync("TournamentStarted", status);

      await StartNextTournamentMatch(tournamentId);
   }

   private async Task StartNextTournamentMatch(string tournamentId)
   {
      var currentMatch = _tournamentService.GetCurrentMatch(tournamentId);
      if (currentMatch == null)
      {
         await EndTournament(tournamentId);
         return;
      }

      var gameType = _tournamentService.GetTournamentStatus(tournamentId)?.GameType ?? "TicTacToe";
      var gameId = $"{tournamentId}_match_{currentMatch.Player1}_{currentMatch.Player2}";
      var matchPlayers = new List<string> { currentMatch.Player1, currentMatch.Player2 };

      if (!_gameService.StartGame(gameId, gameType, matchPlayers))
      {
         await Clients.Group(tournamentId).SendAsync("Error", "Failed to start match.");
         return;
      }

      await Clients.Group(tournamentId).SendAsync("TournamentMatchStarted", new
      {
         tournamentId,
         gameId,
         player1 = currentMatch.Player1,
         player2 = currentMatch.Player2,
         gameType,
         initialState = _gameService.GetGameState(gameId)
      });
   }

   public async Task RecordTournamentMatchResult(string tournamentId, string gameId, string? winner, bool isDraw = false)
   {
      if (!_tournamentService.RecordMatchResult(tournamentId, winner, isDraw))
      {
         await Clients.Group(tournamentId).SendAsync("Error", "Failed to record match result.");
         return;
      }

      _gameService.RemoveGame(gameId);

      var updatedStatus = _tournamentService.GetTournamentStatus(tournamentId);
      await Clients.Group(tournamentId).SendAsync("TournamentMatchComplete", new
      {
         tournamentId,
         winner,
         isDraw,
         leaderboard = updatedStatus?.Leaderboard
      });

      _tournamentService.AdvanceToNextMatch(tournamentId);

      if (_tournamentService.IsTournamentComplete(tournamentId))
      {
         await EndTournament(tournamentId);
      }
      else
      {
         await StartNextTournamentMatch(tournamentId);
      }
   }

   public Task<TournamentStatus?> GetTournamentStatus(string tournamentId)
   {
      return Task.FromResult(_tournamentService.GetTournamentStatus(tournamentId));
   }

   private async Task EndTournament(string tournamentId)
   {
      var leaderboard = _tournamentService.GetLeaderboard(tournamentId);

      await Clients.Group(tournamentId).SendAsync("TournamentComplete", new
      {
         tournamentId,
         leaderboard,
         winner = leaderboard.FirstOrDefault()
      });

      _tournamentService.RemoveTournament(tournamentId);
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
      if (_lobbyService.TryGetGameId(code, playerName, out var gameId) && gameId is not null)
      {
         if (_gameService.MakeMove(gameId, moveData, out var newState))
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
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Services;

enum State
{
   Empty,
   X,
   O
}
public class TicTacToeHub : Hub
{
   private static ConcurrentDictionary<string, GameState> _games = new();

   public async Task StartGame(string gameID, bool isPlayerX)
   {
      if (!_games.ContainsKey(gameID))
         _games[gameID] = new GameState();

      await Groups.AddToGroupAsync(Context.ConnectionId, gameID);
      await Clients.Caller.SendAsync("GameUpdate", _games[gameID]);
   }

   public async Task MakeMove(string gameID, int x, int y, bool isPlayerX)
   {
      if (_games.TryGetValue(gameID, out var game))
      {
         if (game.ApplyMove(x, y, isPlayerX))
         {
            await Clients.Group(gameID).SendAsync("GameUpdate", game);
         }
      }
   }
}

public class GameState
{
   public int[][] Board { get; set; } = new int[3][]
    {
        [0,0,0],
        [0,0,0],
        [0,0,0]
    };
   public bool IsXTurn { get; set; } = true;
   public string? Winner { get; set; } = null;

   public bool ApplyMove(int x, int y, bool isPlayerX)
   {
      if (Winner != null) return false;
      if (Board[x][y] != (int)State.Empty) return false;
      if ((IsXTurn && !isPlayerX) || (!IsXTurn && isPlayerX)) return false;

      Board[x][y] = isPlayerX ? (int)State.X : (int)State.O;
      CheckWinner();
      IsXTurn = !IsXTurn;
      return true;
   }

   private void CheckWinner()
   {
      int[][] b = Board;
      int[] lines = new int[8]; // 3 rows, 3 collumns, 2 diagonals

      for (int i = 0; i < 3; ++i)
      {
         for (int j = 0; j < 3; ++j)
         {
            lines[i] = b[i][j];
            lines[i + 3] = b[j][i];
         }
         lines[6] = b[0][0] + b[1][1] + b[2][2];
         lines[7] = b[0][2] + b[1][1] + b[2][0];

      }
      foreach (var line in lines)
      {
         if (line == 3) Winner = "X";
         if (line == 6) Winner = "O";
      }
   }
}
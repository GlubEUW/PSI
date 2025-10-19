using System.Text.Json;

namespace Api.GameLogic;

public enum State
{
   Empty,
   X,
   O
}

public class TicTacToeGame : IGame
{
   public string GameType => "TicTacToe";

   public Dictionary<string, State> Players { get; set; } = new();
   public int[][] Board { get; set; } = new int[3][]
   {
      new int[3],
      new int[3],
      new int[3]
   };

   public string? PlayerTurn { get; set; } // Which player's turn it is currently
   public string? Winner { get; set; } = null;

   public TicTacToeGame(List<string> players)
   {
      PlayerTurn = players.FirstOrDefault();
      Players[players[0]] = State.X;
      Players[players[1]] = State.O;
   }

   public object GetState()
   {
      return new { Board, PlayerTurn, Winner };
   }

   public bool MakeMove(JsonElement moveData)
   {
      try
      {
         // moveData will be { playerName, x, y }
         var move = moveData.Deserialize<TicTacToeMove>();
         return ApplyMove(move.PlayerName, move.X, move.Y);
      }
      catch (JsonException)
      {
         return false;
      }
   }

   private bool ApplyMove(string playerName, int x, int y)
   {
      if (Winner != null) 
         return false;
      if (Board[x][y] != (int)State.Empty) 
         return false;
      if (playerName != PlayerTurn) 
         return false;

      Board[x][y] = (int)Players[playerName];
      
      CheckWinner();
      if (Winner == "X")
         Winner = Players.FirstOrDefault(p => p.Value == State.X).Key;
      else if (Winner == "O")
         Winner = Players.FirstOrDefault(p => p.Value == State.O).Key;
         
      PlayerTurn = Players.Keys.FirstOrDefault(name => name != playerName);

      return true;
   }

   private void CheckWinner()
   {
      int[][] b = Board;

      // Check rows
      for (int i = 0; i < 3; i++)
      {
         if(b[i][0] == (int)State.X && b[i][1] == (int)State.X && b[i][2] == (int)State.X)
         {
            Winner = "X";
            return;
         }
         if(b[i][0] == (int)State.O && b[i][1] == (int)State.O && b[i][2] == (int)State.O)
         {
            Winner = "O";
            return;
         }
      }

      // Check columns
      for (int i = 0; i < 3; i++)
      {
         if(b[0][i] == (int)State.X && b[1][i] == (int)State.X && b[2][i] == (int)State.X)
         {
            Winner = "X";
            return;
         }
         if(b[0][i] == (int)State.O && b[1][i] == (int)State.O && b[2][i] == (int)State.O)
         {
            Winner = "O";
            return;
         }
      }

      // Check diagonals
      if((b[0][0] == (int)State.X && b[1][1] == (int)State.X && b[2][2] == (int)State.X) ||
         (b[0][2] == (int)State.X && b[1][1] == (int)State.X && b[2][0] == (int)State.X))
      {
         Winner = "X";
         return;
      }
      if((b[0][0] == (int)State.O && b[1][1] == (int)State.O && b[2][2] == (int)State.O) ||
         (b[0][2] == (int)State.O && b[1][1] == (int)State.O && b[2][0] == (int)State.O))
      {
         Winner = "O";
         return;
      }

      bool isDraw = true;
      for (int i = 0; i < 3; i++)
         for (int j = 0; j < 3; j++)
            if (b[i][j] == (int)State.Empty)
               isDraw = false;

      if (isDraw)
         Winner = "Draw";
   }

   public string? GetWinner() => Winner;
}

// Helper struct for move data
public struct TicTacToeMove 
{
   required public string PlayerName { get; set; }
   public int X { get; set; }
   public int Y { get; set; }
}
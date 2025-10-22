using System.Text.Json;
using Api.Entities;

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

   public List<User> Players { get; set; }
   public Dictionary<Guid, State> PlayerSigns { get; set; } = new();
   public int[][] Board { get; set; } = new int[3][]
   {
      new int[3],
      new int[3],
      new int[3]
   };

   public Guid? PlayerTurn { get; set; } // Which player's turn it is currently
   public string? Winner { get; set; } = null;

   public TicTacToeGame(List<User> players)
   {
      Players = players;

      PlayerTurn = players[0].Id;

      PlayerSigns[Players[0].Id] = State.X;
      PlayerSigns[Players[1].Id] = State.O;
   }

   public object GetState()
   {
      User? CurrentPlayer = Players.FirstOrDefault(p => p.Id == PlayerTurn);
      return new
      {
         Board,
         PlayerTurn = CurrentPlayer?.Name,
         Winner,
         WinCounts = Players.Select(p => p.Wins).ToList()
      };
   }

   public bool MakeMove(JsonElement moveData)
   {
      try
      {
         var move = moveData.Deserialize<TicTacToeMove>();
         return ApplyMove(move.PlayerId, move.X, move.Y);
      }
      catch (JsonException)
      {
         return false;
      }
   }

   private bool ApplyMove(Guid playerId, int x, int y)
   {
      if (Winner is not null) 
         return false;
      if (Board[x][y] != (int)State.Empty) 
         return false;
      if (playerId != PlayerTurn) 
         return false;

      Board[x][y] = (int)PlayerSigns[playerId];
      
      CheckWinner();
      if (Winner == "X")
      {
         Winner = Players[0].Name;
         Players[0].Wins++;
      }
      else if (Winner == "O")
      {
         Winner = Players[1].Name;
         Players[1].Wins++;
      }
      
      PlayerTurn = PlayerSigns.Keys.FirstOrDefault(id => id != playerId);
      
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
}

public struct TicTacToeMove 
{
   required public Guid PlayerId { get; set; }
   public int X { get; set; }
   public int Y { get; set; }
}
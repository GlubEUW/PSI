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

   public Dictionary<Guid, State> Players { get; set; } = new();
   public Dictionary<Guid, string> PlayerNamesById { get; set; } = new();
   public int[][] Board { get; set; } = new int[3][]
   {
      new int[3],
      new int[3],
      new int[3]
   };

   public Guid? PlayerTurn { get; set; } // Which player's turn it is currently
   public char? WinnerSign { get; set; } = null;
   public Guid? Winner { get; set; } = null;

   public TicTacToeGame(List<Guid> playerIds, List<string> playerNames)
   {
      PlayerTurn = playerIds.FirstOrDefault();
      Players[playerIds[0]] = State.X;
      Players[playerIds[1]] = State.O;
      PlayerNamesById[playerIds[0]] = playerNames[0];
      PlayerNamesById[playerIds[1]] = playerNames[1];
   }

   public object GetState()
   {
      return new
      {
         Board,
         PlayerTurn = PlayerTurn.HasValue ? PlayerNamesById[PlayerTurn.Value] : null,
         Winner = WinnerSign switch
         {
            'X' => PlayerNamesById[Players.First(p => p.Value == State.X).Key],
            'O' => PlayerNamesById[Players.First(p => p.Value == State.O).Key],
            'D' => "Draw",
            _   => null
         }
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

      Board[x][y] = (int)Players[playerId];
      
      CheckWinner();
         
      PlayerTurn = Players.Keys.FirstOrDefault(name => name != playerId);
      
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
            WinnerSign = 'X';
            return;
         }
         if(b[i][0] == (int)State.O && b[i][1] == (int)State.O && b[i][2] == (int)State.O)
         {
            WinnerSign = 'O';
            return;
         }
      }

      // Check columns
      for (int i = 0; i < 3; i++)
      {
         if(b[0][i] == (int)State.X && b[1][i] == (int)State.X && b[2][i] == (int)State.X)
         {
            WinnerSign = 'X';
            return;
         }
         if(b[0][i] == (int)State.O && b[1][i] == (int)State.O && b[2][i] == (int)State.O)
         {
            WinnerSign = 'O';
            return;
         }
      }

      // Check diagonals
      if((b[0][0] == (int)State.X && b[1][1] == (int)State.X && b[2][2] == (int)State.X) ||
         (b[0][2] == (int)State.X && b[1][1] == (int)State.X && b[2][0] == (int)State.X))
      {
         WinnerSign = 'X';
         return;
      }
      if((b[0][0] == (int)State.O && b[1][1] == (int)State.O && b[2][2] == (int)State.O) ||
         (b[0][2] == (int)State.O && b[1][1] == (int)State.O && b[2][0] == (int)State.O))
      {
         WinnerSign = 'O';
         return;
      }

      bool isDraw = true;
      for (int i = 0; i < 3; i++)
         for (int j = 0; j < 3; j++)
            if (b[i][j] == (int)State.Empty)
               isDraw = false;

      if (isDraw)
         WinnerSign = 'D';
   }
}

public struct TicTacToeMove 
{
   required public Guid PlayerId { get; set; }
   public int X { get; set; }
   public int Y { get; set; }
}
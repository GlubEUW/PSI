using System.Text.Json;

using Api.Enums;
using Api.Exceptions;

namespace Api.GameLogic;

public class TicTacToeGame : IGame
{
   private enum State
   {
      Empty,
      X,
      O
   }

   private struct TicTacToeMove
   {
      required public Guid PlayerId { get; set; }
      public int X { get; set; }
      public int Y { get; set; }
   }
   public GameType GameType => GameType.TicTacToe;
   private Dictionary<Guid, State> PlayerSigns { get; set; } = new();
   private Dictionary<State, Guid> SignsPlayer { get; set; } = new();
   private int[][] Board { get; set; } = [new int[3], new int[3], new int[3]];
   private Guid PlayerTurn { get; set; }
   private Guid? Winner { get; set; } = null;

   public TicTacToeGame(List<Guid> players)
   {
      SignsPlayer[State.X] = players[0];
      SignsPlayer[State.O] = players[1];

      PlayerSigns[SignsPlayer[State.X]] = State.X;
      PlayerSigns[SignsPlayer[State.O]] = State.O;

      PlayerTurn = players[0];
   }

   public object GetState()
   {
      var currentPlayer = SignsPlayer.FirstOrDefault(p => p.Value == PlayerTurn).Value;
      return new
      {
         Board,
         PlayerTurn = currentPlayer,
         Winner
      };
   }

   public bool MakeMove(JsonElement moveData)
   {
      if (Winner is not null)
         return false;

      if (!moveData.TryDeserialize(out TicTacToeMove move))
         throw new MoveNotDeserialized(moveData);

      if (move.PlayerId != PlayerTurn)
         return false;

      return TryMove(move.PlayerId, move.X, move.Y);
   }

   private bool TryMove(Guid playerId, int x, int y)
   {

      if (x < 0 || x >= 3 || y < 0 || y >= 3)
         throw new InvalidMoveException($"Cell ({x}, {y}) is out of bounds (valid: 0-2)", playerId);

      if (Board[x][y] != (int)State.Empty)
         throw new InvalidMoveException($"Cell ({x}, {y}) is already occupied", playerId);

      Board[x][y] = (int)PlayerSigns[playerId];

      CheckWinner();

      if (Winner == "X")
      {
         Winner = Players[0];
      }
      else if (Winner == "O")
      {
         Winner = Players[1];
      }

      PlayerTurn = PlayerSigns.FirstOtherKey(playerId);
      return true;
   }

   private State? CheckWinner()
   {
      foreach (var s in new[] { State.X, State.O })
      {
         for (var i = 0; i < 3; i++)
         {
            if (Board.IsRowEqual(i, s) || Board.IsColumnEqual(i, s))
            {
               return s;
            }
         }
         if (Board.IsDiagonalEqual(s))
         {
            return s;
         }
      }

      if (Board.IsBoardFull())
         return State.Empty;
      return null;
   }
}

public static class TicTacToeExtensions
{
   public static bool IsRowEqual(this int[][] board, int row, State s)
   {
      return board[row].All(cell => cell == (int)s);
   }

   public static bool IsColumnEqual(this int[][] board, int col, State s)
   {
      return board.All(row => row[col] == (int)s);
   }

   public static bool IsDiagonalEqual(this int[][] board, State s)
   {
      return (board[0][0] == (int)s && board[1][1] == (int)s && board[2][2] == (int)s) ||
             (board[0][2] == (int)s && board[1][1] == (int)s && board[2][0] == (int)s);
   }

   public static bool IsBoardFull(this int[][] board)
   {
      return board.All(row => row.All(cell => cell != (int)State.Empty));
   }

   public static TKey FirstOtherKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey keyToExclude) where TKey : notnull
   {
      return dict.Keys.First(k => !k.Equals(keyToExclude));
   }

   public static bool TryDeserialize<T>(this JsonElement element, out T? result)
   {
      try
      {
         result = element.Deserialize<T>();
         return true;
      }
      catch (JsonException)
      {
         result = default;
         return false;
      }
   }
}

using System.Text.Json;

using Api.Enums;
using Api.Exceptions;

namespace Api.GameLogic;

public class ConnectFourGame : IGame
{
   private enum Color
   {
      Empty = 0,
      Red = 1,
      Yellow = 2
   }
   private struct ConnectFourMove
   {
      required public Guid PlayerId { get; set; }
      public int Column { get; set; }
   }

   private static readonly int _rows = 6;
   private static readonly int _cols = 7;

   private Dictionary<Guid, Color> PlayerColors { get; set; } = new();
   private Dictionary<Color, Guid> ColorsPlayers { get; set; } = new();
   private Color[][] Board { get; set; } = new Color[_rows][];
   private Guid PlayerTurn { get; set; }
   private Guid? Winner { get; set; } = null;

   public GameType GameType => GameType.ConnectFour;

   public ConnectFourGame(List<Guid> players)
   {
      PlayerTurn = players[0];

      ColorsPlayers[Color.Red] = players[0];
      ColorsPlayers[Color.Yellow] = players[1];

      PlayerColors[ColorsPlayers[Color.Red]] = Color.Red;
      PlayerColors[ColorsPlayers[Color.Yellow]] = Color.Yellow;

      for (var i = 0; i < 6; i++)
      {
         Board[i] = new Color[7];
      }
   }

   public object GetState()
   {
      return new
      {
         Board,
         PlayerTurn,
         Winner
      };
   }

   public bool MakeMove(JsonElement moveData)
   {
      if (Winner is not null)
         return false;


      if (!moveData.TryDeserialize(out ConnectFourMove move))
         throw new MoveNotDeserialized(moveData);

      if (move.PlayerId != PlayerTurn)
         return false;

      return TryMove(move.PlayerId, move.Column);
   }

   private bool TryMove(Guid playerId, int column)
   {
      if (column < 0 || column >= 7)
         throw new InvalidMoveException($"Column {column} is out of bounds (valid: 0-6)", playerId);

      var row = 5;
      for (; row >= 0; row--)
      {
         if (Board[row][column] == Color.Empty)
         {
            break;
         }
      }

      if (row == -1)
         return false;

      Board[row][column] = PlayerColors[playerId];

      var winnerColor = CheckWinner(row, column);
      if (winnerColor == Color.Red)
      {
         Winner = ColorsPlayers[Color.Red];
      }
      else if (winnerColor == Color.Yellow)
      {
         Winner = ColorsPlayers[Color.Yellow];
      }
      else if (winnerColor == Color.Empty)
      {
         Winner = null;
      }

      PlayerTurn = PlayerColors.FirstOtherKey(playerId);

      return true;
   }

   private Color? CheckWinner(int lastRow, int lastCol)
   {
      var color = Board[lastRow][lastCol];

      if (CheckDirection(lastRow, lastCol, 0, 1, color) ||
          CheckDirection(lastRow, lastCol, 0, -1, color))
      {
         return color;
      }

      if (CheckDirection(lastRow, lastCol, 1, 0, color) ||
          CheckDirection(lastRow, lastCol, -1, 0, color))
      {
         return color;
      }

      if (CheckDirection(lastRow, lastCol, 1, 1, color) ||
          CheckDirection(lastRow, lastCol, -1, -1, color))
      {
         return color;
      }

      if (CheckDirection(lastRow, lastCol, 1, -1, color) ||
          CheckDirection(lastRow, lastCol, -1, 1, color))
      {
         return color;
      }

      if (IsBoardFull())
      {
         return Color.Empty;
      }
      return null;
   }

   private bool CheckDirection(int row, int col, int dRow, int dCol, Color color)
   {
      var count = 1;

      for (var i = 1; i <= 3; i++)
      {
         var r = row + dRow * i;
         var c = col + dCol * i;
         if ((uint)r >= _rows || (uint)c >= _cols || Board[r][c] != color)
            break;
         count++;
         if (count == 4) return true;
      }

      for (var i = 1; i <= 3; i++)
      {
         var r = row - dRow * i;
         var c = col - dCol * i;
         if ((uint)r >= _rows || (uint)c >= _cols || Board[r][c] != color)
            break;
         count++;
         if (count == 4) return true;
      }

      return count >= 4;
   }

   private bool IsBoardFull()
   {
      for (var r = 0; r < _rows; r++)
      {
         var rowArr = Board[r];
         for (var c = 0; c < _cols; c++)
         {
            if (rowArr[c] == Color.Empty)
               return false;
         }
      }
      return true;
   }
}

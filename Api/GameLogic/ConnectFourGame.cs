using System.Text.Json;

using Api.Entities;
using Api.Enums;
using Api.Exceptions;

namespace Api.GameLogic;

public class ConnectFourGame : IGame
{
   private enum _discColor
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

   private List<User> Players { get; set; }
   private Dictionary<User, _discColor> PlayerColors { get; set; } = new();
   private int[][] Board { get; set; } = new int[6][];
   private User PlayerTurn { get; set; }
   private User? Winner { get; set; } = null;

   public GameType GameType => GameType.ConnectFour;

   public ConnectFourGame(List<User> players)
   {
      Players = players;
      PlayerTurn = players[0];

      PlayerColors[Players[0]] = _discColor.Red;
      PlayerColors[Players[1]] = _discColor.Yellow;

      for (var i = 0; i < 6; i++)
      {
         Board[i] = new int[7];
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

      if (move.PlayerId != PlayerTurn.Id)
         return false;

      return ApplyMove(move.PlayerId, move.Column);
   }

   private bool ApplyMove(Guid playerId, int column)
   {
      if (column < 0 || column >= 7)
         throw new InvalidMoveException($"Column {column} is out of bounds (valid: 0-6)", playerId);

      var row = -1;
      for (var r = 5; r >= 0; r--)
      {
         if (Board[r][column] == (int)DiscColor.Empty)
         {
            row = r;
            break;
         }
      }

      if (row == -1)
         return false;

      Board[row][column] = (int)PlayerColors[playerId];

      CheckWinner(row, column);

      if (Winner == "Red")
      {
         Winner = Players[0].Name;
         Players[0].Wins++;
         Players[0].PlayedAndWonGamesByType[Enums.GameType.ConnectFour].Wins++;
         Players[0].PlayedAndWonGamesByType[Enums.GameType.ConnectFour].GamesPlayed++;
         Players[1].PlayedAndWonGamesByType[Enums.GameType.ConnectFour].GamesPlayed++;
      }
      else if (Winner == "Yellow")
      {
         Winner = Players[1].Name;
         Players[1].Wins++;
         Players[1].PlayedAndWonGamesByType[Enums.GameType.ConnectFour].Wins++;
         Players[0].PlayedAndWonGamesByType[Enums.GameType.ConnectFour].GamesPlayed++;
         Players[1].PlayedAndWonGamesByType[Enums.GameType.ConnectFour].GamesPlayed++;
      }

      PlayerTurn = PlayerColors.FirstOtherKey(playerId);

      return true;
   }

   private void CheckWinner(int lastRow, int lastCol)
   {
      var color = Board[lastRow][lastCol];

      if (CheckDirection(lastRow, lastCol, 0, 1, color) ||
          CheckDirection(lastRow, lastCol, 0, -1, color))
      {
         Winner = ((DiscColor)color).ToString();
         return;
      }

      if (CheckDirection(lastRow, lastCol, 1, 0, color) ||
          CheckDirection(lastRow, lastCol, -1, 0, color))
      {
         Winner = ((DiscColor)color).ToString();
         return;
      }

      if (CheckDirection(lastRow, lastCol, 1, 1, color) ||
          CheckDirection(lastRow, lastCol, -1, -1, color))
      {
         Winner = ((DiscColor)color).ToString();
         return;
      }

      if (CheckDirection(lastRow, lastCol, 1, -1, color) ||
          CheckDirection(lastRow, lastCol, -1, 1, color))
      {
         Winner = ((DiscColor)color).ToString();
         return;
      }

      if (Board.IsBoardFull())
      {
         Winner = "Draw";
      }
   }

   private bool CheckDirection(int row, int col, int dRow, int dCol, int color)
   {
      var count = 1;

      var r = row + dRow;
      var c = col + dCol;
      while (r >= 0 && r < 6 && c >= 0 && c < 7 && Board[r][c] == color)
      {
         count++;
         r += dRow;
         c += dCol;
      }

      r = row - dRow;
      c = col - dCol;
      while (r >= 0 && r < 6 && c >= 0 && c < 7 && Board[r][c] == color)
      {
         count++;
         r -= dRow;
         c -= dCol;
      }

      return count >= 4;
   }
}
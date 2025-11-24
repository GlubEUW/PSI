using System.Text.Json;

using Api.Entities;
using Api.Enums;
using Api.Exceptions;

namespace Api.GameLogic;

public class ConnectFourGame : IGame
{
   private enum Color
   {
      Red = 0,
      Yellow = 1,
      Empty = 2
   }
   private record struct ConnectFourMove
   {
      required public User Player { get; set; }
      public int Column { get; set; }
   }

   private static readonly int _rows = 6;
   private static readonly int _cols = 7;

   private readonly User[] _players = new User[2];
   private int _turnIndex;
   private User? Winner { get; set; }
   private Color[][] _board = new Color[_rows][];

   public GameType GameType => GameType.ConnectFour;

   public ConnectFourGame(List<User> players)
   {
      if (players.Count != 2) throw new InvalidOperationException("ConnectFour requires exactly 2 players.");
      _players[0] = players[0];
      _players[1] = players[1];
      _turnIndex = 0;
      for (var r = 0; r < _rows; r++)
      {
         var rowArr = new Color[_cols];
         for (var c = 0; c < _cols; c++) rowArr[c] = Color.Empty;
         _board[r] = rowArr;
      }
   }

   public object GetState()
   {
      return new { Board = _board, PlayerTurn = _players[_turnIndex], Winner };
   }

   public bool MakeMove(JsonElement moveData)
   {
      if (Winner is not null) return false;
      ConnectFourMove move;
      try { move = JsonSerializer.Deserialize<ConnectFourMove>(moveData.GetRawText()); }
      catch (JsonException) { throw new MoveNotDeserialized(moveData); }
      if (move.Player != _players[_turnIndex]) return false;
      return ApplyMove(move.Player, move.Column);
   }

   private bool ApplyMove(User player, int column)
   {
      if ((uint)column >= _cols) throw new InvalidMoveException($"Column {column} is out of bounds (valid: 0-{_cols - 1})", player.Id);
      var row = _rows - 1;
      while (row >= 0 && _board[row][column] != Color.Empty) row--;
      if (row < 0) return false;
      var color = player == _players[0] ? Color.Red : Color.Yellow;
      _board[row][column] = color;
      var winnerColor = EvaluateWinner(row, column);
      if (winnerColor == Color.Red) Winner = _players[0];
      else if (winnerColor == Color.Yellow) Winner = _players[1];
      else if (winnerColor == Color.Empty) Winner = null;
      _turnIndex ^= 1;
      return true;
   }

   private Color? EvaluateWinner(int row, int col)
   {
      var color = _board[row][col];
      if (color == Color.Empty) return null;
      if (HasLine(row, col, 0, 1, color) || HasLine(row, col, 1, 0, color) || HasLine(row, col, 1, 1, color) || HasLine(row, col, 1, -1, color)) return color;
      return IsBoardFull() ? Color.Empty : null;
   }

   private bool HasLine(int row, int col, int dRow, int dCol, Color color)
   {
      var count = 1;
      for (var i = 1; i <= 3; i++) { var r = row + dRow * i; var c = col + dCol * i; if ((uint)r >= _rows || (uint)c >= _cols || _board[r][c] != color) break; if (++count == 4) return true; }
      for (var i = 1; i <= 3; i++) { var r = row - dRow * i; var c = col - dCol * i; if ((uint)r >= _rows || (uint)c >= _cols || _board[r][c] != color) break; if (++count == 4) return true; }
      return false;
   }

   private bool IsBoardFull()
   {
      for (var r = 0; r < _rows; r++)
      {
         var arr = _board[r];
         for (var c = 0; c < _cols; c++) if (arr[c] == Color.Empty) return false;
      }
      return true;
   }
}

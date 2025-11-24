using System.Text.Json;

using Api.Entities;
using Api.Enums;
using Api.Exceptions;

namespace Api.GameLogic;

public class TicTacToeGame : IGame
{
   private enum Mark
   {
      X = 0,
      O = 1,
      Empty = 2
   }
   private record struct TicTacToeMove
   {
      public required User Player { get; set; }
      public int X { get; set; }
      public int Y { get; set; }
   }
   public GameType GameType => GameType.TicTacToe;
   private readonly User[] _players = new User[2];
   private int _turnIndex;
   private Mark[][] _board = new Mark[3][];
   public User? Winner { get; set; }

   public TicTacToeGame(List<User> players)
   {
      if (players.Count != 2) throw new InvalidOperationException("TicTacToe requires exactly 2 players.");
      _players[(int)Mark.X] = players[0];
      _players[(int)Mark.O] = players[1];
      _turnIndex = 0;
      for (var r = 0; r < 3; r++)
      {
         var row = new Mark[3];
         for (var c = 0; c < 3; c++) row[c] = Mark.Empty;
         _board[r] = row;
      }
   }

   public object GetState()
   {
      var boardOut = new int[3][];
      for (var r = 0; r < 3; r++)
      {
         var row = new int[3];
         for (var c = 0; c < 3; c++) row[c] = (int)_board[r][c];
         boardOut[r] = row;
      }
      return new { Board = boardOut, PlayerTurn = _players[_turnIndex], Winner };
   }

   public bool MakeMove(JsonElement moveData)
   {
      if (Winner is not null) return false;
      TicTacToeMove move;
      try { move = JsonSerializer.Deserialize<TicTacToeMove>(moveData.GetRawText()); }
      catch (JsonException) { throw new MoveNotDeserialized(moveData); }
      if (move.Player != _players[_turnIndex]) return false;
      return ApplyMove(move.Player, move.X, move.Y);
   }

   private bool ApplyMove(User player, int x, int y)
   {
      if ((uint)x >= 3 || (uint)y >= 3) throw new InvalidMoveException($"Cell ({x}, {y}) is out of bounds (valid: 0-2)", player.Id);
      if (_board[x][y] != Mark.Empty) throw new InvalidMoveException($"Cell ({x}, {y}) is already occupied", player.Id);
      var mark = _turnIndex == 0 ? Mark.X : Mark.O;
      _board[x][y] = mark;
      var result = EvaluateWinner(x, y);
      if (result == Mark.X) Winner = _players[(int)Mark.X];
      else if (result == Mark.O) Winner = _players[(int)Mark.O];
      else if (result == Mark.Empty) Winner = null;
      _turnIndex ^= 1;
      return true;
   }

   private Mark? EvaluateWinner(int x, int y)
   {
      var mark = _board[x][y];
      if (mark == Mark.Empty) return null;
      if (RowWin(x, mark) || ColWin(y, mark) || DiagWin(mark)) return mark;
      return IsBoardFull() ? Mark.Empty : null;
   }

   private bool RowWin(int row, Mark mark)
   {
      for (var c = 0; c < 3; c++) if (_board[row][c] != mark) return false; return true;
   }
   private bool ColWin(int col, Mark mark)
   {
      for (var r = 0; r < 3; r++) if (_board[r][col] != mark) return false; return true;
   }
   private bool DiagWin(Mark mark)
   {
      var a = _board[0][0] == mark && _board[1][1] == mark && _board[2][2] == mark;
      var b = _board[0][2] == mark && _board[1][1] == mark && _board[2][0] == mark;
      return a || b;
   }
   private bool IsBoardFull()
   {
      for (var r = 0; r < 3; r++) for (var c = 0; c < 3; c++) if (_board[r][c] == Mark.Empty) return false; return true;
   }
}

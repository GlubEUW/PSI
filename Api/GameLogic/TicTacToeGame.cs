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

   public string? PlayerTurn { get; set; }
   public string? Winner { get; set; } = null;

   public BoardEnumerator AllCells => new BoardEnumerator(Board);

   private static readonly List<List<(int row, int col)>> WinningLines = new()
   {
      // Rows
      new() { (0, 0), (0, 1), (0, 2) },
      new() { (1, 0), (1, 1), (1, 2) },
      new() { (2, 0), (2, 1), (2, 2) },
      // Columns
      new() { (0, 0), (1, 0), (2, 0) },
      new() { (0, 1), (1, 1), (2, 1) },
      new() { (0, 2), (1, 2), (2, 2) },
      // Diagonals
      new() { (0, 0), (1, 1), (2, 2) },
      new() { (0, 2), (1, 1), (2, 0) }
   };

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
      // Check for winner using LINQ query syntax
      var winningState = (from line in WinningLines
                          let cellValues = (from pos in line
                                            join cell in AllCells
                                            on new { pos.row, pos.col } equals new { cell.row, cell.col }
                                            select cell.value).ToList()
                          where cellValues.Count == 3 &&
                                cellValues[0] != State.Empty &&
                                cellValues.All(v => v == cellValues[0])
                          select cellValues[0]).FirstOrDefault();

      if (winningState != State.Empty)
      {
         Winner = winningState.ToString();
         return;
      }

      // Check for draw using LINQ query syntax
      var emptyCells = from cell in AllCells
                       where cell.value == State.Empty
                       select cell;

      if (!emptyCells.Any())
         Winner = "Draw";
   }

   public string? GetWinner() => Winner;
}

public struct TicTacToeMove
{
   required public string PlayerName { get; set; }
   public int X { get; set; }
   public int Y { get; set; }
}
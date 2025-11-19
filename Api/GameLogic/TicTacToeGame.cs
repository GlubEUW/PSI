using System.Text.Json;

using Api.Entities;

namespace Api.GameLogic;

public enum State
{
   Empty,
   X,
   O
}

public struct TicTacToeMove
{
   required public Guid PlayerId { get; set; }
   public int X { get; set; }
   public int Y { get; set; }
}

public class TicTacToeGame : IGame
{
   public string GameType => "TicTacToe";
   public List<User> Players { get; set; }
   public Dictionary<Guid, State> PlayerSigns { get; set; } = new();
   public int[][] Board { get; set; } = new int[3][] { new int[3], new int[3], new int[3] };
   public Guid? PlayerTurn { get; set; }
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
      var currentPlayer = Players.FirstOrDefault(p => p.Id == PlayerTurn);
      return new
      {
         Board,
         PlayerTurn = currentPlayer?.Name,
         Winner,
         WinCounts = Players.Select(p => p.Wins).ToList()
      };
   }

   public bool MakeMove(JsonElement moveData)
   {
      if (!moveData.TryDeserialize(out TicTacToeMove move))
         return false;

      return ApplyMove(move.PlayerId, move.X, move.Y);
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
         Players[0].PlayedAndWonGamesByType[Enums.GameType.TicTacToe].Wins++;
         Players[0].PlayedAndWonGamesByType[Enums.GameType.TicTacToe].GamesPlayed++;
         Players[1].PlayedAndWonGamesByType[Enums.GameType.TicTacToe].GamesPlayed++;
      }
      else if (Winner == "O")
      {
         Winner = Players[1].Name;
         Players[1].Wins++;
         Players[1].PlayedAndWonGamesByType[Enums.GameType.TicTacToe].Wins++;
         Players[0].PlayedAndWonGamesByType[Enums.GameType.TicTacToe].GamesPlayed++;
         Players[1].PlayedAndWonGamesByType[Enums.GameType.TicTacToe].GamesPlayed++;
      }

      PlayerTurn = PlayerSigns.FirstOtherKey(playerId);
      return true;
   }

   private void CheckWinner()
   {
      foreach (var s in new[] { State.X, State.O })
      {
         for (var i = 0; i < 3; i++)
         {
            if (Board.IsRowEqual(i, s) || Board.IsColumnEqual(i, s))
            {
               Winner = s.ToString();
               return;
            }
         }
         if (Board.IsDiagonalEqual(s))
         {
            Winner = s.ToString();
            return;
         }
      }

      if (Board.IsBoardFull())
         Winner = "Draw";
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

using System.Text.Json;

namespace Api.GameLogic;

public enum RpsChoice
{
   None = 0,
   Rock = 1,
   Paper = 2,
   Scissors = 3
}

public class RpsGame : IGame
{
   public string GameType => "RockPaperScissors";

   public Dictionary<string, RpsChoice> Players { get; set; } = new();
   public string? Result { get; set; }

   public RpsGame(List<string> players)
   {
      if (players == null || players.Count < 2)
      {
         Players["Player1"] = RpsChoice.None;
         Players["Player2"] = RpsChoice.None;
      }
      else
      {
         Players[players[0]] = RpsChoice.None;
         Players[players[1]] = RpsChoice.None;
      }
   }

   public object GetState()
   {
      return new { Players, Result };
   }

   public bool MakeMove(JsonElement moveData)
   {
      // moveData will be { playerName, choice }
      var move = moveData.Deserialize<RpsMove>();
      if (move == null)
         return false;

      Players[move.PlayerName] = move.Choice;

      // Check if both players made a choice
      if (Players.Count == 2 && !Players.ContainsValue(RpsChoice.None)) // Suggestion: refactor to foreach
         Result = DetermineWinner();

      return true;
   }

   private string DetermineWinner()
   {
      var players = Players.Keys.ToList();
      var p1 = players[0];
      var p2 = players[1];
      var c1 = Players[p1];
      var c2 = Players[p2];

      if (c1 == c2) 
         return "Draw!";

      if ((c1 == RpsChoice.Rock && c2 == RpsChoice.Scissors) ||
          (c1 == RpsChoice.Paper && c2 == RpsChoice.Rock) ||
          (c1 == RpsChoice.Scissors && c2 == RpsChoice.Paper))
         return $"{p1} wins!";

      return $"{p2} wins!";
   }

   public string? GetWinner() => Result;
}

// Helper class for move data
public class RpsMove
{
   public required string PlayerName { get; set; }
   public RpsChoice Choice { get; set; }
}
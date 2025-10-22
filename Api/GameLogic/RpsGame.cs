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

   public Dictionary<Guid, RpsChoice> Players { get; set; } = new();
   public Dictionary<Guid, string> PlayerNamesById { get; set; } = new();
   public Guid? Winner { get; set; }
   public string? Result { get; set; }

   public RpsGame(List<Guid> playerIds, List<string> playerNames)
   {
      Players[playerIds[0]] = RpsChoice.None;
      Players[playerIds[1]] = RpsChoice.None;
      PlayerNamesById[playerIds[0]] = playerNames[0];
      PlayerNamesById[playerIds[1]] = playerNames[1];
   }

   public object GetState()
   {
      return new
      {
         Players,
         Result
      };
   }

   public bool MakeMove(JsonElement moveData)
   {
      try
      {
         var move = moveData.Deserialize<RpsMove>();
         Players[move.PlayerId] = move.Choice;

         // Check if both players made a choice
         if (Players.Count == 2 && !Players.ContainsValue(RpsChoice.None)) // Suggestion: refactor to foreach
         {
            Winner = DetermineWinner();
            if (Winner is null)
               Result = "Draw!";

            else
               Result = $"{PlayerNamesById[Winner.Value]} wins!";
         }

         return true;
      }
      catch (JsonException)
      {
         return false;
      }
   }

   private Guid? DetermineWinner()
   {
      var players = Players.Keys.ToList();
      var p1 = players[0];
      var p2 = players[1];
      var c1 = Players[p1];
      var c2 = Players[p2];

      if (c1 == c2)
         return null;

      if ((c1 == RpsChoice.Rock && c2 == RpsChoice.Scissors) ||
          (c1 == RpsChoice.Paper && c2 == RpsChoice.Rock) ||
          (c1 == RpsChoice.Scissors && c2 == RpsChoice.Paper))
         return p1;

      return p2;
   }
}

public struct RpsMove
{
   public required Guid PlayerId { get; set; }
   public RpsChoice Choice { get; set; }
}
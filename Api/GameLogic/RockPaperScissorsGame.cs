using System.Text.Json;

using Api.Entities;

namespace Api.GameLogic;

public enum RockPaperScissorsChoice
{
   None = 0,
   Rock = 1,
   Paper = 2,
   Scissors = 3
}

public struct RockPaperScissorsMove
{
   public required Guid PlayerId { get; set; }
   public RockPaperScissorsChoice Choice { get; set; }
}
public class RockPaperScissorsGame : IGame
{
   public string GameType => "RockPaperScissors";
   public List<User> Players { get; set; }
   public Dictionary<Guid, RockPaperScissorsChoice> PlayerChoices { get; set; } = new();
   public Guid? Winner { get; set; }
   public string? Result { get; set; }

   public RockPaperScissorsGame(List<User> players)
   {
      Players = players;
      PlayerChoices[players[0].Id] = RockPaperScissorsChoice.None;
      PlayerChoices[players[1].Id] = RockPaperScissorsChoice.None;
   }

   public object GetState()
   {
      return new
      {
         Players,
         Result,
         WinCounts = Players.Select(p => p.Wins).ToList()
      };
   }

   public bool MakeMove(JsonElement moveData)
   {
      try
      {
         var move = moveData.Deserialize<RockPaperScissorsMove>();
         PlayerChoices[move.PlayerId] = move.Choice;

         // Check if both players made a choice
         if (PlayerChoices.Count == 2 && !PlayerChoices.ContainsValue(RockPaperScissorsChoice.None))
            Result = DetermineWinner();

         return true;
      }
      catch (JsonException)
      {
         return false;
      }
   }

   private string? DetermineWinner()
   {
      // var players = PlayerChoices.Keys.ToList();
      var p1 = Players[0].Id;
      var p2 = Players[1].Id;
      var c1 = PlayerChoices[p1];
      var c2 = PlayerChoices[p2];

      if (c1 == c2)
         return "Draw!";

      if ((c1 == RockPaperScissorsChoice.Rock && c2 == RockPaperScissorsChoice.Scissors) ||
          (c1 == RockPaperScissorsChoice.Paper && c2 == RockPaperScissorsChoice.Rock) ||
          (c1 == RockPaperScissorsChoice.Scissors && c2 == RockPaperScissorsChoice.Paper))
      {
         Players[0].Wins++;
         return $"{Players[0].Name} wins!";
      }

      Players[1].Wins++;
      return $"{Players[1].Name} wins!";
   }
}

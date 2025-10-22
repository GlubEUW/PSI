using System.Text.Json;
using Api.Entities;

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
   public List<User> Players { get; set; }
   public Dictionary<Guid, RpsChoice> PlayerChoices { get; set; } = new();
   public Guid? Winner { get; set; }
   public string? Result { get; set; }

   public RpsGame(List<User> players)
   {
      Players = players;
      PlayerChoices[players[0].Id] = RpsChoice.None;
      PlayerChoices[players[1].Id] = RpsChoice.None;
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
         var move = moveData.Deserialize<RpsMove>();
         PlayerChoices[move.PlayerId] = move.Choice;

         // Check if both players made a choice
         if (PlayerChoices.Count == 2 && !PlayerChoices.ContainsValue(RpsChoice.None))
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

      if ((c1 == RpsChoice.Rock && c2 == RpsChoice.Scissors) ||
          (c1 == RpsChoice.Paper && c2 == RpsChoice.Rock) ||
          (c1 == RpsChoice.Scissors && c2 == RpsChoice.Paper))
      {
         Players[0].Wins++;
         return $"{Players[0].Name} wins!";
      }

      Players[1].Wins++;
      return $"{Players[1].Name} wins!";
   }
}

public struct RpsMove
{
   public required Guid PlayerId { get; set; }
   public RpsChoice Choice { get; set; }
}
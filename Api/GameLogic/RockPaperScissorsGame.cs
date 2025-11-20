using System.Text.Json;

using Api.Enums;

namespace Api.GameLogic;

public class RockPaperScissorsGame : IGame
{
   private enum RockPaperScissorsChoice
   {
      Rock = 1,
      Paper = 2,
      Scissors = 3
   }

   private struct RockPaperScissorsMove
   {
      public required Guid PlayerId { get; set; }
      public RockPaperScissorsChoice Choice { get; set; }
   }
   public GameType GameType => GameType.RockPaperScissors;
   private readonly List<Guid> _players;
   private Dictionary<Guid, RockPaperScissorsChoice?> PlayerChoices { get; set; } = new();
   private Guid? Winner { get; set; }
   public string? Result { get; set; }

   public RockPaperScissorsGame(List<Guid> players)
   {
      _players = players;
      PlayerChoices[players[0]] = null;
      PlayerChoices[players[1]] = null;
   }

   public object GetState()
   {
      return new
      {
         Winner,
         Result
      };
   }

   public bool MakeMove(JsonElement moveData)
   {
      try
      {
         var move = moveData.Deserialize<RockPaperScissorsMove>();
         PlayerChoices[move.PlayerId] = move.Choice;

         if (PlayerChoices.Count == 2 && !PlayerChoices.ContainsValue(null))
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
      var c1 = PlayerChoices[_players[0]];
      var c2 = PlayerChoices[_players[1]];

      if (c1 == c2)
         return "Draw!";

      if ((c1 == RockPaperScissorsChoice.Rock && c2 == RockPaperScissorsChoice.Scissors) ||
          (c1 == RockPaperScissorsChoice.Paper && c2 == RockPaperScissorsChoice.Rock) ||
          (c1 == RockPaperScissorsChoice.Scissors && c2 == RockPaperScissorsChoice.Paper))
      {
         return $"{_players[0]} wins!";
      }

      return $"{_players[1]} wins!";
   }
}

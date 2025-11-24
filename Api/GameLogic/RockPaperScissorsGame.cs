using System.Text.Json;

using Api.Enums;

namespace Api.GameLogic;

public class RockPaperScissorsGame : IGame
{
   private enum RockPaperScissorsChoice
   {
      Rock = 0,
      Paper = 1,
      Scissors = 2
   }
   private struct RockPaperScissorsMove
   {
      public required Guid PlayerId { get; set; }
      public RockPaperScissorsChoice Choice { get; set; }
   }
   public GameType GameType => GameType.RockPaperScissors;
   private readonly Guid[] _players = new Guid[2];
   private RockPaperScissorsChoice?[] _choices = new RockPaperScissorsChoice?[2];
   private Guid? Winner { get; set; }
   public string? Result { get; set; }

   public RockPaperScissorsGame(List<Guid> players)
   {
      if (players.Count != 2) throw new InvalidOperationException("RockPaperScissors requires exactly 2 players.");
      _players[0] = players[0];
      _players[1] = players[1];
      _choices[0] = null;
      _choices[1] = null;
   }

   public object GetState()
   {
      return new { Winner, Result };
   }

   public bool MakeMove(JsonElement moveData)
   {
      RockPaperScissorsMove move;
      try { move = moveData.Deserialize<RockPaperScissorsMove>(); }
      catch (JsonException) { return false; }
      var idx = move.PlayerId == _players[0] ? 0 : (move.PlayerId == _players[1] ? 1 : -1);
      if (idx < 0) return false;
      _choices[idx] = move.Choice;
      if (_choices[0].HasValue && _choices[1].HasValue) Result = DetermineWinner();
      return true;
   }

   private string? DetermineWinner()
   {
      var c1 = _choices[0];
      var c2 = _choices[1];
      if (c1 == c2) return "Draw!";
      if ((c1 == RockPaperScissorsChoice.Rock && c2 == RockPaperScissorsChoice.Scissors) ||
          (c1 == RockPaperScissorsChoice.Paper && c2 == RockPaperScissorsChoice.Rock) ||
          (c1 == RockPaperScissorsChoice.Scissors && c2 == RockPaperScissorsChoice.Paper)) return $"{_players[0]} wins!";
      return $"{_players[1]} wins!";
   }
}

using System.Text.Json;

using Api.Entities;
using Api.Enums;

namespace Api.GameLogic;

public class RockPaperScissorsGame : IGame
{
   public User? Winner { get; private set; }
   public bool GameOver { get; private set; } = false;
   private string? Result { get; set; }
   public List<User> Players { get; private set; }
   private enum RockPaperScissorsChoice
   {
      Rock = 0,
      Paper = 1,
      Scissors = 2
   }
   private struct RockPaperScissorsMove
   {
      public RockPaperScissorsChoice Choice { get; set; }
   }
   public GameType GameType => GameType.RockPaperScissors;
   private RockPaperScissorsChoice?[] _choices = new RockPaperScissorsChoice?[2];
   public RockPaperScissorsGame(List<User> players)
   {
      if (players.Count != 2) throw new InvalidOperationException("RockPaperScissors requires exactly 2 players.");
      Players = players;
      _choices[0] = null;
      _choices[1] = null;
   }

   public object GetState()
   {
      return new { Winner, Result, GameOver };
   }

   public bool MakeMove(JsonElement moveData, User player)
   {
      RockPaperScissorsMove move;
      try { move = moveData.Deserialize<RockPaperScissorsMove>(); }
      catch (JsonException) { return false; }
      var idx = player == Players[0] ? 0 : (player == Players[1] ? 1 : -1);
      if (idx < 0) return false;
      _choices[idx] = move.Choice;
      if (_choices[0].HasValue && _choices[1].HasValue)
      {
         GameOver = true;
         Result = EvaluateWinner();
      }
      return true;
   }

   private string? EvaluateWinner()
   {
      var c1 = _choices[0];
      var c2 = _choices[1];
      if (c1 == c2) return "Draw!";
      if ((c1 == RockPaperScissorsChoice.Rock && c2 == RockPaperScissorsChoice.Scissors) ||
          (c1 == RockPaperScissorsChoice.Paper && c2 == RockPaperScissorsChoice.Rock) ||
          (c1 == RockPaperScissorsChoice.Scissors && c2 == RockPaperScissorsChoice.Paper)) return $"{Players[0]} wins!";
      return $"{Players[1]} wins!";
   }
}

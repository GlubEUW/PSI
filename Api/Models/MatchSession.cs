using Api.Entities;

namespace Api.Models;

public class MatchSession
{
   public required string Code { get; set; }
   public List<User> Players { get; set; } = new();
   public string? GameType { get; set; }
   public List<string> GamesList { get; set; } = new();
   public int NumberOfRounds { get; set; } = 1;
   public int CurrentRound { get; set; } = 0;
   public bool InGame { get; set; }

   public readonly Dictionary<Guid, string> _gameIdByUserId = new();
}
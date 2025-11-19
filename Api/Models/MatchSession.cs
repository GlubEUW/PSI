using Api.Entities;

using System.Collections.Concurrent;
namespace Api.Models;

public class MatchSession
{
   public Guid Id { get; set; }
   public required string Code { get; set; }
   public List<User> Players { get; set; } = new();
   public List<List<User>> PlayerGroups { get; set; } = new();
   public string? GameType { get; set; } // FIXME: Change to enum
   public List<string> GamesList { get; set; } = new(); // FIXME: Change to enum
   public int NumberOfRounds { get; set; } = 1;
   public int CurrentRound { get; set; } = 0;
   public bool InGame { get; set; }
   public readonly Dictionary<Guid, string> _gameIdByUserId = new();
   public ConcurrentDictionary<int, HashSet<string>> EndedGamesByRound { get; set; } = new();
}
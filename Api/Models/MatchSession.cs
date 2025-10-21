namespace Api.Models;

public class MatchSession
{
   public required string Code { get; set; }
   public List<string> Players { get; set; } = new();
   public string? GameType { get; set; }
   public List<string> GamesList { get; set; } = new();

   public int CurrentRound { get; set; } = 0;
   public bool InGame { get; set; }

   public readonly Dictionary<string, string> _gameIdByPlayerName = new();
}
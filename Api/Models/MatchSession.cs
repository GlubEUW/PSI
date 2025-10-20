namespace Api.Models;

public class MatchSession
{
   public required string Code { get; set; }
   public List<string> Players { get; set; } = new();
   public bool InGame { get; set; }
   public readonly Dictionary<string, string> _gameIdByPlayerName = new();
}

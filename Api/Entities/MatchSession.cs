namespace Api.Entities;

public class MatchSession
{
   public required string Code { get; set; }
   public List<string> Players { get; set; } = new();
   public string? GameType { get; set; }
   public bool inGame { get; set; }
   public List<string> GamesList { get; set; } = new();
}

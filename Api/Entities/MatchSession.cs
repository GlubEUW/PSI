namespace Api.Entities;

public class MatchSession
{
   public required string Code { get; set; }
   public List<string> Players { get; set; } = new();
   public string? GameType { get; set; }
   public bool inGame { get; set; }
   //for lobby creator
   public int NumberOfRounds { get; set; } = 1;
   public List<string> GamesList { get; set; } = new();
   public int MaxPlayers { get; set; } = 2;
   public int CurrentRound { get; set; } = 0;
}

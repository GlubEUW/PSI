namespace Api.Entities;

public class MatchSession
{
   public required string Code { get; set; }
   public List<string> Players { get; set; } = new();
   public bool InGame { get; set; } // Not sure if we need this
}

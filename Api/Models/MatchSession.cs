namespace Api.Models;

public class MatchSession // FIXME: refactor to Entities group
{
   public required string Code { get; set; }
   public List<string> Players { get; set; } = new();
   public string? GameType { get; set; }
   public bool inGame { get; set; }
}

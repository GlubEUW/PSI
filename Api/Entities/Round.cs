namespace Api.Entities;

public class Round
{
   public Guid Id { get; set; }
   public Guid MatchId { get; set; }
   public string GameType { get; set; } = string.Empty;
   public int RoundNumber { get; set; }
   public Guid Winner { get; set; }
}
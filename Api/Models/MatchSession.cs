namespace PSI.Api.Models
{
   public class MatchSession
   {
      public required string Code { get; set; }
      public List<string> Players { get; set; } = new();
      public bool inGame { get; set; }
   }
}

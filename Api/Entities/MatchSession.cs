namespace Api.Entities;

public class MatchSession
{
   public required string Code { get; set; }
   public List<string> Players { get; set; } = new(); // FIXME: Change to list of Users instead of playernames to incorporate GuId
   public bool InGame { get; set; } // Not sure if we need this
   public readonly Dictionary<string, string> _gameIdByPlayerName = new(); // FIXME: Change to map by userId later on
}

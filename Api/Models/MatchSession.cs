public class MatchSession
{
    public required string Code { get; set; }
    public required string HostConnectionId { get; set; }
    public List<string> Players { get; set; } = new();
}
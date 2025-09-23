public class GameSession
{
    public string Code { get; set; }
    public string HostConnectionId { get; set; }
    public List<string> Players { get; set; } = new();
}

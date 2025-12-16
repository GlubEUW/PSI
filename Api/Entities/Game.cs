namespace Api.Entities;

public class Game
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public string GameType { get; set; } = string.Empty;
    public short RoundNumber { get; set; }
}
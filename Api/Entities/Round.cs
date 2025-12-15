namespace Api.Entities;

public class Round
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public string GameType { get; set; } = string.Empty;
    public short RoundNumber { get; set; }
}
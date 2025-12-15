namespace Api.Entities;

public class UserRound
{
    public Guid UserId { get; set; }
    public Guid RoundId { get; set; }
    public short PlayerTurn { get; set; }
    public short PlayerPlacement { get; set; }
}
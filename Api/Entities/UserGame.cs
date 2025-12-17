namespace Api.Entities;

public class UserGame
{
   public Guid UserId { get; set; }
   public Guid GameId { get; set; }
   public short PlayerTurn { get; set; }
   public short PlayerPlacement { get; set; }
}

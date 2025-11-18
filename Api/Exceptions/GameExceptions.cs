namespace Api.Exceptions;

public class GameException(string message, string? gameId = null) : Exception(message)
{
   public string? GameId { get; } = gameId;
   public DateTime OccurredAt { get; } = DateTime.UtcNow;

}

public class InvalidMoveException(string reason, Guid playerId, string? gameId = null) : GameException($"Invalid move by player {playerId}: {reason}", gameId)
{
    public Guid PlayerId { get; } = playerId;
    public string Reason { get; } = reason;
}

public class GameNotFoundException(string gameId) : GameException($"Game with ID '{gameId}' was not found", gameId);
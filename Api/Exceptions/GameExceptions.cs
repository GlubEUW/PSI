using System.Text.Json;

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

public class MoveNotDeserialized(JsonElement moveData) : Exception($"Move could not be deserialized from data: {moveData.GetRawText()}")
{
   public JsonElement MoveData { get; } = moveData;
}

public class PlayerNotFoundException(Guid playerId, string? context = null)
   : Exception($"Player {playerId} not found in {context ?? "game"}")
{
   public Guid PlayerId { get; } = playerId;
   public string? Context { get; } = context;
}

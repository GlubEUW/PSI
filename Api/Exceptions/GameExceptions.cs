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

public class LobbyException(string message, string? lobbyCode = null) : Exception(message)
{
    public string? LobbyCode { get; } = lobbyCode;
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public class LobbyFullException(string lobbyCode, int currentPlayers, int maxPlayers) : LobbyException($"Lobby '{lobbyCode}' is full ({currentPlayers}/{maxPlayers} players)", lobbyCode)
{
    public int MaxPlayers { get; } = maxPlayers;
    public int CurrentPlayers { get; } = currentPlayers;
}

public class PlayerNotFoundException(Guid playerId, string? lobbyCode = null) : LobbyException($"Player {playerId} not found in lobby {lobbyCode ?? "unknown"}", lobbyCode)
{
    public Guid PlayerId { get; } = playerId;
}
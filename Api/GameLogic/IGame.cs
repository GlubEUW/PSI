namespace Api.GameLogic;

public interface IGame
{
    string GameType { get; }
    object GetState();
    bool MakeMove(string playerId, object moveData);
    string? GetWinner();
}
using System.Text.Json;

namespace Api.GameLogic;

public interface IGame
{
    string GameType { get; }
    object GetState();
    bool MakeMove(JsonElement moveData);
}
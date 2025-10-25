using System.Text.Json;

namespace Api.GameLogic;

public interface IGame
{
   public string GameType { get; }
   public object GetState();
   public bool MakeMove(JsonElement moveData);
}

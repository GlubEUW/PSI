using System.Text.Json;

using Api.Enums;

namespace Api.GameLogic;

public interface IGame
{
   //potentialy public Guid winner { get; }
   public GameType GameType { get; }
   public object GetState();
   public bool MakeMove(JsonElement moveData);
}

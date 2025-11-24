using System.Text.Json;

using Api.Enums;
using Api.Exceptions;

namespace Api.GameLogic;

public interface IGame
{
   //potentialy public Guid winner { get; }
   public GameType GameType { get; }
   public object GetState();
   public bool MakeMove(JsonElement moveData);
}

public static class GameExtensions
{
   public static T DeserializeOrThrow<T>(this JsonElement element)
   {
      try
      {
         return JsonSerializer.Deserialize<T>(element.GetRawText()) ?? throw new MoveNotDeserialized(element);
      }
      catch (JsonException)
      {
         throw new MoveNotDeserialized(element);
      }
   }
}

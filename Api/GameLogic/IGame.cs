using System.Text.Json;

using Api.Entities;
using Api.Enums;

namespace Api.GameLogic;

public interface IGame
{
   public User? Winner { get; }
   public GameType GameType { get; }
   public bool GameOver { get; }
   public List<User> Players { get; }

   public object GetState();
   public bool MakeMove(JsonElement moveData, User player);
}

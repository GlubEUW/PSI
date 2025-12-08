using Api.Entities;
using Api.Enums;
using Api.GameLogic;

namespace Api.Services;

public interface IGameService
{
   public bool IsValidGameType(string gameName);
   public bool IsValidGameType(GameType gameType);
   public IGame? StartGame(GameType gameType, List<User> players);
}

using Api.Entities;
using Api.Enums;
using Api.GameLogic;

namespace Api.Services;

public interface IGameService
{
   public IGame? StartGame(GameType gameType, List<User> players);
}

using Api.Entities;

namespace Api.Services;

public interface ILobbyService
{
   public List<User> GetPlayersInLobby(string code);
   public Task<string?> JoinLobby(string code, User user);
   public Task<bool> LeaveLobby(string code, Guid userId);
   public Task<string> CreateLobbyWithSettings(int numberOfPlayers, int numberOfRounds, bool randomGames, List<string>? gamesList);
   public string? CanJoinLobby(string code, Guid userId);
}

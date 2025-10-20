using Api.Models;

namespace Api.Services;

public interface ILobbyService
{
   public bool AddGameId(string code, string playerName, string gameId = "");
   public bool RemoveGameId(string? code, string? playerName);
   public bool TryGetGameId(string? code, string? playerName, out string? gameId);
   public List<string> GetPlayersInLobby(string code);
   public LobbyInfoDto GetLobbyInfo(string code);
   public Task<bool> CreateMatch(string code);
   public Task<string?> JoinMatch(string code, string playerName);
   public Task<bool> LeaveMatch(string code, string playerName);

   public string? CanJoinLobby(string code, string playerName);
}
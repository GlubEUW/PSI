using Api.Models;

namespace Api.Services;

public interface ILobbyService
{
    public List<string> GetPlayersInLobby(string code);
    public LobbyInfoDto GetLobbyInfo(string code);
    public Task<bool> CreateMatch(string code);
    public Task<string?> JoinMatch(string code, string playerName);
    public Task<bool> LeaveMatch(string code, string playerName);

    public string? CanJoinLobby(string code, string playerName);
}
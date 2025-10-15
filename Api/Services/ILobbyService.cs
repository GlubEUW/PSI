using Api.Services;
using Api.Models;

namespace Api.Services;

public interface ILobbyService
{
    public bool IsLobbyFull(string code);
    public bool IsNameTakenInLobby(string code, string name);
    public List<string> GetPlayersInLobby(string code);
    public LobbyInfoDto GetLobbyInfo(string code, string name);
    public Task<bool> CreateMatch(string code);
    public Task<bool> JoinMatch(string code, string playerName);
    public Task<bool> LeaveMatch(string code, string playerName);
}
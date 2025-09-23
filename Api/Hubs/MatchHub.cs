using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class MatchmakingHub : Hub
{
    private static ConcurrentDictionary<string, GameSession> _sessions = new();
    private static int _maxPlayersPerSession = 2;
    public async Task<string> CreateGame(string hostName,string code)
    {
        _sessions[code] = new GameSession
        {
            Code = code,
            HostConnectionId = Context.ConnectionId,
            Players = new List<string> { hostName }
        };

        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        return code;
    }

    public async Task<bool> JoinGame(string code, string playerName)
    {
        if (_sessions.TryGetValue(code, out var session) && session.Players.Count < _maxPlayersPerSession)
        {
            session.Players.Add(playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            await Clients.Group(code).SendAsync("PlayerJoined", new { name = playerName, connectionId = Context.ConnectionId });
            return true;
        }
        return false;
    }

    public async Task StartGame(string code)
    {
        if (_sessions.ContainsKey(code) &&  _sessions[code].Players.Count == 2)
        {
            await Clients.Group(code).SendAsync("GameStarted");
        }
    }

}



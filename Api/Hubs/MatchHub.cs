using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class MatchHub : Hub
{
    private static ConcurrentDictionary<string, MatchSession> _sessions = new();
    public async Task<string> CreateMatch(string code)
    {
        if (_sessions.ContainsKey(code))
        {
            return null;
        }
        _sessions[code] = new MatchSession
        {
            Code = code,
            HostConnectionId = Context.ConnectionId,
            Players = new List<string>(2)
        };

        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        return code;
    }

    public async Task<bool> JoinMatch(string code, string playerName)
    {
        if (_sessions.TryGetValue(code, out var session) && session.Players.Count < session.Players.Capacity)
        {
            session.Players.Add(playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            return true;
        }
        return false;
    }

    public async Task StartMatch(string code)
    {
        if (_sessions.ContainsKey(code) && _sessions[code].Players.Count == 2)
        {
            await Clients.Group(code).SendAsync("MatchStarted");
        }
    }

    public List<string> GetPlayers(string code)
    {
        if (_sessions.TryGetValue(code, out var session))
        {
            return session.Players;
        }
        return new List<string>();
    }
}



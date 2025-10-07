using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class MatchHub : Hub
{
    private static ConcurrentDictionary<string, MatchSession> _sessions = new();
    public async Task<MatchSession> CreateMatch(string code)
    {
        _sessions[code] = new MatchSession
        {
            Code = code,
            HostConnectionId = Context.ConnectionId,
            Players = new List<string>(2)
        };
        Console.WriteLine($"Match created with code: {code}");
        return _sessions[code];
    }

    [Authorize]
    public async Task<bool> JoinMatch(string code, string playerToken)
    {
        Context.Items["Code"] = code;
        if (_sessions.TryGetValue(code, out var session) != true)
        {
            session = await CreateMatch(code);
        }
        if (session.Players.Contains(playerToken))
        {
            return true;
        }
        if (session.Players.Count < session.Players.Capacity)
        {
            session.Players.Add(playerToken);
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            await Clients.Group(code).SendAsync("PlayersUpdated", session.Players);
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

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Connection {Context.ConnectionId} disconnecting.");
        if (Context.Items.TryGetValue("Code", out var codeObj) && codeObj is string code)
        {
            if (_sessions.TryGetValue(code, out var session))
            {
                session.Players.RemoveAll(p => p == Context.ConnectionId);
                if (session.Players.Count == 0)
                {
                    _sessions.TryRemove(code, out _);
                    Console.WriteLine($"Match with code {code} removed due to no players.");
                }
                else
                {
                    await Clients.Group(code).SendAsync("PlayersUpdated", session.Players);
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public List<string> GetPlayers(string code)
    {
        if (_sessions.TryGetValue(code, out var session))
        {
            //extract names from tokens
            var names = new List<string>();
            foreach (var token in session.Players)
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name);
                if (nameClaim != null)
                    names.Add(nameClaim.Value);
            }
            return names;
        }
        return new List<string>();
    }
}



using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class MatchHub : Hub
{
    private static ConcurrentDictionary<string, MatchSession> _sessions = new();
    public async Task<string?> CreateMatch(string code)
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

    public async Task<bool> JoinMatch(string code, string playerToken)
    {
        if (_sessions.TryGetValue(code, out var session) && session.Players.Count < session.Players.Capacity)
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



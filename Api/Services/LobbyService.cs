using System.Collections.Concurrent;
using Api.Models;
using Api.Entities;


namespace Api.Services;

public class LobbyService() : ILobbyService
{
    private static ConcurrentDictionary<string, MatchSession> _sessions = new();
    public bool IsLobbyFull(string code)
    {
        if (_sessions.TryGetValue(code, out var session) && session != null)
        {
            return session.Players.Count >= session.Players.Capacity;
        }
        return false;
    }

    public List<string> GetPlayersInLobby(string code)
    {
        if (_sessions.TryGetValue(code, out var session) && session != null)
        {
            return new List<string>(session.Players);
        }
        return new List<string>();
    }

    public bool IsNameTakenInLobby(string code, string name)
    {
        if (_sessions.TryGetValue(code, out var session) && session != null)
        {
            return session.Players.Contains(name);
        }
        return false;
    }
    public JoinLobbyResponseDto GetJoinLobbyInfo(string code, string name)
    {
        return new JoinLobbyResponseDto
        {
            IsLobbyFull = IsLobbyFull(code),
            IsNameTakenInLobby = IsNameTakenInLobby(code, name)
        };
    }

    public Task<bool> CreateMatch(string code)
    {
        if (!_sessions.ContainsKey(code))
        {
            _sessions[code] = new MatchSession
            {
                Code = code,
                Players = new List<string>(2),
                inGame = false
            };
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
    public Task<bool> JoinMatch(string code, string playerName)
    {
        if (!_sessions.TryGetValue(code, out var session))
        {
            CreateMatch(code);
            session = _sessions[code];
        }

        if (session.Players.Contains(playerName))
        {
            return Task.FromResult(false);
        }

        if (session.Players.Count < session.Players.Capacity)
        {
            session.Players.Add(playerName);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> LeaveMatch(string code, string playerName)
    {
        if (_sessions.TryGetValue(code, out var session))
        {
            session.Players.Remove(playerName);
            if (session.Players.Count == 0)
            {
                _sessions.TryRemove(code, out _);
                return Task.FromResult(true);
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using PSI.Api.Models;

public class MatchHub : Hub
{
    public static ConcurrentDictionary<string, MatchSession> _sessions = new(); // Public so LobbyControllers could access it
    public MatchSession CreateMatch(string code)
    {
        _sessions[code] = new MatchSession
        {
            Code = code,
            Players = new List<string>(2),
            inGame = false
        };
        Console.WriteLine($"Match created with code: {code}");
        return _sessions[code];
    }

    public async Task<bool> JoinMatch(string code, string playerName)
    {
        Context.Items["Code"] = code;
        Context.Items["PlayerName"] = playerName;
        if (!_sessions.TryGetValue(code, out var session))
        {
            session = CreateMatch(code);
        }

        if (session.Players.Contains(playerName))
        {
            return false; // Player name already taken in this match
        }

        if (session.Players.Count < session.Players.Capacity)
        {
            session.Players.Add(playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            await Clients.Group(code).SendAsync("PlayersUpdated", session.Players);
            Console.WriteLine($"{playerName} joined match {code}");
            return true;
        }

        Console.WriteLine($"{playerName} failed to join match {code} — match full.");
        return false;
    }

    public async Task StartMatch(string selectedGameType, string code)
    {
        Console.WriteLine($"StartMatch called with GameType={selectedGameType}, Code={code}");

        if (_sessions.ContainsKey(code))
        {
            Console.WriteLine($"Found session for code {code} with {_sessions[code].Players.Count} players");
        }

        if (_sessions.ContainsKey(code) /*&& _sessions[code].Players.Count == 2*/)
        {
            _sessions[code].GameType = selectedGameType; // Set the game type
            Console.WriteLine($"Starting match for {code}...");
            await Clients.Group(code).SendAsync("MatchStarted", new { gameType = selectedGameType });
        }
        else
        {
            Console.WriteLine($"Cannot start match: session missing or not enough players.");
        }
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("Code", out var codeObj) && codeObj is string code)
        {
            if (_sessions.TryGetValue(code, out var session))
            {
                string? playerName = null;
                if (Context.Items.TryGetValue("PlayerName", out var playerObj) && playerObj is string p)
                {
                    playerName = p;
                }

                if (playerName != null)
                {
                    session.Players.RemoveAll(p => p == playerName);
                    Console.WriteLine($"{playerName} disconnected from match {code}.");
                }
                else
                {
                    Console.WriteLine($"Unknown player disconnected from match {code}.");
                }

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
            return new List<string>(session.Players);
        }

        return new List<string>();
    }
}


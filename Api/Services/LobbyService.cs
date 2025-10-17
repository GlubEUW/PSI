using System.Collections.Concurrent;
using Api.Models;
using Api.Entities;

namespace Api.Services;

public class LobbyService() : ILobbyService
{
   private static ConcurrentDictionary<string, MatchSession> _sessions = new();
   public string? CanJoinLobby(string code, string playerName)
   {
      if (_sessions.TryGetValue(code, out var session) && session != null)
      {
         if (session.inGame)
         {
            return "Game already started.";
         }
         if (session.Players.Count >= session.Players.Capacity)
         {
            return "Lobby is full.";
         }
         if (session.Players.Contains(playerName))
         {
            return "Name already taken.";
         }
         return null;
      }
      return "Game does not exist.";
   }

   public List<string> GetPlayersInLobby(string code)
   {
      if (_sessions.TryGetValue(code, out var session) && session != null)
      {
         return new List<string>(session.Players);
      }
      return new List<string>();
   }

   public LobbyInfoDto GetLobbyInfo(string code)
   {
      return new LobbyInfoDto
      {
         Players = GetPlayersInLobby(code)
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

   public Task<string?> JoinMatch(string code, string playerName)
   {
      if (!_sessions.TryGetValue(code, out var session))
      {
         CreateMatch(code);
         session = _sessions[code];
      }
      var result = CanJoinLobby(code, playerName);
      if (result is null)
      {
         session.Players.Add(playerName);
      }
      return Task.FromResult(result);
   }

   public Task<bool> LeaveMatch(string code, string playerName)
   {
      if (_sessions.TryGetValue(code, out var session))
      {
         session.Players.Remove(playerName);
         if (session.Players.Count == 0)
         {
            return Task.FromResult(_sessions.TryRemove(code, out _));
         }
         return Task.FromResult(true);
      }
      return Task.FromResult(false);
   }
}
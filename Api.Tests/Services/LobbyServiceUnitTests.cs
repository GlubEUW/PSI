using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Api.Services;
using Api.Entities;
using Api.Models;

namespace Api.Tests.Services;

public class LobbyServiceUnitTests
{
   private static async Task<(LobbyService service, string code)> NewLobby(int capacity = 2)
   {
      var svc = new LobbyService();
      var code = Guid.NewGuid().ToString("N").Substring(0, 6);
      // create via CreateLobbyWithSettings to set capacity and initialize
      var createdCode = await svc.CreateLobbyWithSettings(capacity, 1, true, null);
      return (svc, createdCode);
   }

   [Fact]
   public async Task CreateMatch_ReturnsTrueThenFalse_OnDuplicate()
   {
      var svc = new LobbyService();
      var code = Guid.NewGuid().ToString("N").Substring(0, 6);
      var first = await svc.CreateMatch(code);
      var second = await svc.CreateMatch(code);
      Assert.True(first);
      Assert.False(second);
   }

   [Fact]
   public void CanJoinLobby_NoSession_ReturnsGameDoesNotExist()
   {
      var svc = new LobbyService();
      var msg = svc.CanJoinLobby("missing", Guid.NewGuid());
      Assert.Equal("Game does not exist.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_WhenFull_ReturnsFull()
   {
      var (svc, code) = await NewLobby(1);
      var session = svc.GetMatchSession(code)!;
      session.Players.Add(new Guest { Id = Guid.NewGuid(), Name = "p1" });
      var msg = svc.CanJoinLobby(code, Guid.NewGuid());
      Assert.Equal("Lobby is full.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_WhenInGame_ReturnsStarted()
   {
      var (svc, code) = await NewLobby(2);
      var session = svc.GetMatchSession(code)!;
      session.InGame = true;
      var msg = svc.CanJoinLobby(code, Guid.NewGuid());
      Assert.Equal("Game already started.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_NameTaken_ReturnsTaken()
   {
      var (svc, code) = await NewLobby(2);
      var id = Guid.NewGuid();
      await svc.JoinMatch(code, new Guest { Id = id, Name = "p" });
      var msg = svc.CanJoinLobby(code, id);
      Assert.Equal("Name already taken.", msg);
   }

   [Fact]
   public async Task JoinMatch_AddsPlayer_AndLeaveRemoves()
   {
      var (svc, code) = await NewLobby(2);
      var id = Guid.NewGuid();
      var err = await svc.JoinMatch(code, new Guest { Id = id, Name = "p" });
      Assert.Null(err);
      Assert.Contains(svc.GetPlayersInLobby(code), u => u.Id == id);

      var left = await svc.LeaveMatch(code, id);
      Assert.True(left);
      Assert.DoesNotContain(svc.GetPlayersInLobby(code), u => u.Id == id);
   }

   [Fact]
   public async Task LeaveMatch_LastPlayer_RemovesSession()
   {
      var (svc, code) = await NewLobby(1);
      var id = Guid.NewGuid();
      await svc.JoinMatch(code, new Guest { Id = id, Name = "p" });
      var result = await svc.LeaveMatch(code, id);
      Assert.True(result);
      Assert.Null(svc.GetMatchSession(code));
   }

   [Fact]
   public async Task GameIdMapping_Add_Get_Remove()
   {
      var (svc, code) = await NewLobby(2);
      var userId = Guid.NewGuid();
      var added = svc.AddGameId(code, userId, code + "_G0_R0");
      Assert.True(added);
      Assert.True(svc.TryGetGameId(code, userId, out var gid));
      Assert.Equal(code + "_G0_R0", gid);
      Assert.True(svc.RemoveGameId(code, userId));
      Assert.False(svc.TryGetGameId(code, userId, out _));
   }

   [Fact]
   public async Task RoundTracking_Reset_Mark_And_AllEnded()
   {
      var (svc, code) = await NewLobby(2);
      var session = svc.GetMatchSession(code)!;
      session.CurrentRound = 0;
      var a = Guid.NewGuid();
      var b = Guid.NewGuid();
      session._gameIdByUserId[a] = code + "_G0_R0";
      session._gameIdByUserId[b] = code + "_G0_R0";

      svc.ResetRoundEndTracking(code);
      Assert.False(svc.AreAllGamesEnded(code));

      svc.MarkGameAsEnded(code, code + "_G0_R0");
      Assert.True(svc.AreAllGamesEnded(code));
   }

   [Fact]
   public async Task GetMatchRoundInfo_ComputesWithinBounds()
   {
      var (svc, code) = await NewLobby(2);
      var session = svc.GetMatchSession(code)!;
      session.NumberOfRounds = 2;
      session.CurrentRound = 1; // 0-based, so reported as 2

      var info = svc.GetMatchRoundInfo(code);
      Assert.Equal(2, info.CurrentRound);
      Assert.Equal(2, info.TotalRounds);
   }
}

using Api.Services;
using Api.Entities;

namespace Api.Tests.Services;

public class LobbyServiceUnitTests
{
   [Fact]
   public void AddGameId_ReturnsFalse_WhenSessionMissing()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var userId = Guid.NewGuid();
      var added = svc.AddGameId("missing", userId, "g0");
      Assert.False(added);
   }

   [Fact]
   public async Task AddGameId_ReturnsFalse_OnDuplicateKey()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var userId = Guid.NewGuid();

      var first = svc.AddGameId(code, userId, code + "_G0_R0");
      Assert.True(first);

      var second = svc.AddGameId(code, userId, code + "_G0_R0");
      Assert.False(second);
   }

   [Fact]
   public async Task AddGameId_UsesCode_WhenGameIdEmpty()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var userId = Guid.NewGuid();

      var added = svc.AddGameId(code, userId, "");
      Assert.True(added);

      var ok = svc.TryGetGameId(code, userId, out var gid);
      Assert.True(ok);
      Assert.Equal(code, gid);
   }

   [Fact]
   public async Task GameIdMapping_Add_Get_Remove()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var userId = Guid.NewGuid();
      var added = svc.AddGameId(code, userId, code + "_G0_R0");
      Assert.True(added);
      Assert.True(svc.TryGetGameId(code, userId, out var gid));
      Assert.Equal(code + "_G0_R0", gid);
      Assert.True(svc.RemoveGameId(code, userId));
      Assert.False(svc.TryGetGameId(code, userId, out _));
   }

   [Fact]
   public void RemoveGameId_ReturnsFalse_WhenCodeOrUserIdNull()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var userId = Guid.NewGuid();

      Assert.False(svc.RemoveGameId(null, userId));
      Assert.False(svc.RemoveGameId("some", null));
   }

   [Fact]
   public void RemoveGameId_ReturnsFalse_WhenSessionMissing()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var userId = Guid.NewGuid();
      Assert.False(svc.RemoveGameId("missing", userId));
   }

   [Fact]
   public async Task TryGetGameId_ReturnsFalse_WhenCodeNull_OrSessionMissing_OrMappingMissing()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var userId = Guid.NewGuid();

      Assert.False(svc.TryGetGameId(null, userId, out var gid1));
      Assert.Null(gid1);

      Assert.False(svc.TryGetGameId("missing", userId, out var gid2));
      Assert.Null(gid2);

      var (lobby2, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc2 = (LobbyService)lobby2;
      Assert.False(svc2.TryGetGameId(code, userId, out var gid3));
      Assert.Null(gid3);
   }

   [Fact]
   public async Task JoinMatch_ReturnsGameDoesNotExist_WhenNoSession()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var res = await svc.JoinMatch("missing", new Guest { Id = Guid.NewGuid(), Name = "x" });
      Assert.Equal("Game does not exist.", res);
   }

   [Fact]
   public async Task JoinMatch_ReturnsError_WhenCannotJoin()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(1, 1, true, null);
      var svc = (LobbyService)lobby;
      var id = Guid.NewGuid();
      await svc.JoinMatch(code, new Guest { Id = id, Name = "p1" });

      var res = await svc.JoinMatch(code, new Guest { Id = Guid.NewGuid(), Name = "p2" });
      Assert.Equal("Lobby is full.", res);
   }

   [Fact]
   public async Task JoinMatch_AddsPlayer_AndLeaveRemoves()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var id = Guid.NewGuid();
      var err = await svc.JoinMatch(code, new Guest { Id = id, Name = "p" });
      Assert.Null(err);
      Assert.Contains(svc.GetPlayersInLobby(code), u => u.Id == id);

      var left = await svc.LeaveMatch(code, id);
      Assert.True(left);
      Assert.DoesNotContain(svc.GetPlayersInLobby(code), u => u.Id == id);
   }

   [Fact]
   public async Task LeaveMatch_ReturnsFalse_WhenSessionMissing()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var res = await svc.LeaveMatch("missing", Guid.NewGuid());
      Assert.False(res);
   }

   [Fact]
   public async Task LeaveMatch_ReturnsTrue_WhenUserNotFound_ButPlayersRemain()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(3, 1, true, null);
      var svc = (LobbyService)lobby;
      var a = Guid.NewGuid();
      var b = Guid.NewGuid();
      await svc.JoinMatch(code, new Guest { Id = a, Name = "a" });
      await svc.JoinMatch(code, new Guest { Id = b, Name = "b" });

      var other = Guid.NewGuid();
      var result = await svc.LeaveMatch(code, other);
      Assert.True(result);
      var players = svc.GetPlayersInLobby(code);
      Assert.Contains(players, p => p.Id == a);
      Assert.Contains(players, p => p.Id == b);
   }

   [Fact]
   public async Task LeaveMatch_LastPlayer_RemovesSession()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(1, 1, true, null);
      var svc = (LobbyService)lobby;
      var id = Guid.NewGuid();
      await svc.JoinMatch(code, new Guest { Id = id, Name = "p" });
      var result = await svc.LeaveMatch(code, id);
      Assert.True(result);
      Assert.Null(svc.GetTournamentSession(code));
   }

   [Fact]
   public void CanJoinLobby_NoSession_ReturnsGameDoesNotExist()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var msg = svc.CanJoinLobby("missing", Guid.NewGuid());
      Assert.Equal("Game does not exist.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_WhenFull_ReturnsFull()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(1, 1, true, null);
      var svc = (LobbyService)lobby;
      var session = svc.GetTournamentSession(code)!;
      session.Players.Add(new Guest { Id = Guid.NewGuid(), Name = "p1" });
      var msg = svc.CanJoinLobby(code, Guid.NewGuid());
      Assert.Equal("Lobby is full.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_WhenInGame_ReturnsStarted()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var session = svc.GetTournamentSession(code)!;
      session.InGame = true;
      var msg = svc.CanJoinLobby(code, Guid.NewGuid());
      Assert.Equal("Game already started.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_NameTaken_ReturnsTaken()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var id = Guid.NewGuid();
      await svc.JoinMatch(code, new Guest { Id = id, Name = "p" });
      var msg = svc.CanJoinLobby(code, id);
      Assert.Equal("Name already taken.", msg);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_UsesProvidedGamesList_WhenRandomFalse()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var games = new List<string> { "TicTacToe", "RockPaperScissors" };
      var code = await svc.CreateLobbyWithSettings(2, 2, false, games);

      var session = svc.GetTournamentSession(code)!;
      Assert.Equal(games, session.GamesList);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_RandomGames_GeneratesCorrectCount_And_ValidTypes()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var code = await svc.CreateLobbyWithSettings(2, 3, true, null);
      var session = svc.GetTournamentSession(code)!;
      Assert.Equal(3, session.GamesList.Count);
      Assert.All(session.GamesList, g => Assert.False(string.IsNullOrWhiteSpace(g)));
   }

   [Fact]
   public async Task GetMatchRoundInfo_Defaults_WhenSessionMissing_And_ClampsCurrentRound()
   {
      var svc = new LobbyService(new Api.Tests.TestDoubles.TestGameFactory());
      var info = svc.GetMatchRoundInfo("missing");
      Assert.Equal(1, info.CurrentRound);
      Assert.Equal(1, info.TotalRounds);

      var (lobby2, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc2 = (LobbyService)lobby2;
      var session = svc2.GetTournamentSession(code)!;
      session.NumberOfRounds = 1;
      session.CurrentRound = 5;

      var info2 = svc2.GetMatchRoundInfo(code);
      Assert.Equal(1, info2.CurrentRound);
      Assert.Equal(1, info2.TotalRounds);
   }

   [Fact]
   public async Task GetMatchRoundInfo_ComputesWithinBounds()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var session = svc.GetTournamentSession(code)!;
      session.NumberOfRounds = 2;
      session.CurrentRound = 1;

      var info = svc.GetMatchRoundInfo(code);
      Assert.Equal(2, info.CurrentRound);
      Assert.Equal(2, info.TotalRounds);
   }

   [Fact]
   public async Task GetMatchRoundInfo_NormalCase_Returns1BasedCurrent()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var session = svc.GetTournamentSession(code)!;
      session.NumberOfRounds = 4;
      session.CurrentRound = 0;

      var info = svc.GetMatchRoundInfo(code);
      Assert.Equal(1, info.CurrentRound);
      Assert.Equal(4, info.TotalRounds);
   }
   [Fact]
   public void MarkGameAsEnded_Noop_WhenSessionMissing()
   {
      var svc = new LobbyService(new Api.GameLogic.GameFactory());
      svc.MarkGameAsEnded("missing", "g");
      Assert.False(svc.AreAllGamesEnded("missing"));
   }

   [Fact]
   public async Task RoundTracking_Reset_Mark_And_AllEnded()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var session = svc.GetTournamentSession(code)!;
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
   public async Task MarkGameAsEnded_CreatesEndedSet_WhenNoneExists_And_AllEndedReflects()
   {
      var (lobby, code) = await TestHelpers.CreateLobbyAsync(2, 1, true, null);
      var svc = (LobbyService)lobby;
      var session = svc.GetTournamentSession(code)!;
      session.CurrentRound = 0;
      var a = Guid.NewGuid();
      var b = Guid.NewGuid();
      session._gameIdByUserId[a] = code + "_G0_R0";
      session._gameIdByUserId[b] = code + "_G0_R0";

      Assert.False(svc.AreAllGamesEnded(code));

      svc.MarkGameAsEnded(code, code + "_G0_R0");

      Assert.True(svc.AreAllGamesEnded(code));
   }
}

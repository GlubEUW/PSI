using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Api.Hubs;
using Api.Services;
using Api.Entities;
using Api.Models;

namespace Api.Tests.Hubs;

public class MatchHubUnitTests
{
   private static (MatchHub hub, Mock<ILobbyService> lobby, Mock<IGameService> game) CreateHub()
   {
      var lobbyMock = new Mock<ILobbyService>();
      var gameMock = new Mock<IGameService>();
      var hub = new MatchHub(lobbyMock.Object, gameMock.Object);
      return (hub, lobbyMock, gameMock);
   }

   private static (object Code, object User) GetContextKeys()
   {
      var t = typeof(MatchHub).GetNestedType("ContextKeys", BindingFlags.NonPublic)!;
      return (Enum.Parse(t, "Code")!, Enum.Parse(t, "User")!);
   }

   private static IDictionary<object, object?> MakeItems(string? code = null, User? user = null)
   {
      var (codeKey, userKey) = GetContextKeys();
      var dict = new Dictionary<object, object?>();
      if (code != null) dict[codeKey] = code;
      if (user != null) dict[userKey] = user;
      return dict;
   }

   private static Mock<HubCallerContext> BuildContext(IDictionary<object, object?> items)
   {
      var ctx = new Mock<HubCallerContext>();
      ctx.Setup(c => c.Items).Returns(items);
      return ctx;
   }

   private static (Mock<IHubCallerClients> clients, Mock<ISingleClientProxy> caller) BuildCallerClients()
   {
      var clients = new Mock<IHubCallerClients>();
      var caller = new Mock<ISingleClientProxy>();
      clients.Setup(c => c.Caller).Returns(caller.Object);
      return (clients, caller);
   }

   private static Mock<ISingleClientProxy> SetupGroup(Mock<IHubCallerClients> clients, string groupId)
   {
      var proxy = new Mock<ISingleClientProxy>();
      clients.Setup(c => c.Group(groupId)).Returns(proxy.Object);
      return proxy;
   }

   [Fact]
   public async Task MakeMove_GroupNotFound_SendsError()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var playerId = Guid.NewGuid();
      var user = TestHelpers.BuildGuest("U", playerId);

      var items = MakeItems("C9", user);
      var ctx = BuildContext(items);

      var session = new MatchSession
      {
         Code = "C9",
         PlayerGroups = new List<List<User>> { new() { TestHelpers.BuildGuest("Other1"), TestHelpers.BuildGuest("Other2") } }
      };
      lobbyMock.Setup(s => s.GetMatchSession("C9")).Returns(session);

      var gid = "C9_G0_R0";
      lobbyMock.Setup(s => s.TryGetGameId("C9", playerId, out gid)).Returns(true);

      object? stateObj = new { ok = true };
      gameMock.Setup(s => s.MakeMove(gid, It.IsAny<JsonElement>(), out stateObj)).Returns(true);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      var json = JsonDocument.Parse("{}");
      await hub.MakeMove(json.RootElement);

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Your player group could not be found."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task MakeMove_InvalidMove_NoNotifications()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var playerId = Guid.NewGuid();
      var user = TestHelpers.BuildGuest("U2", playerId);

      var items = MakeItems("C10", user);
      var ctx = BuildContext(items);

      var session = new MatchSession
      {
         Code = "C10",
         PlayerGroups = new List<List<User>> { new() { user } }
      };
      lobbyMock.Setup(s => s.GetMatchSession("C10")).Returns(session);

      var gid = "C10_G0_R0";
      lobbyMock.Setup(s => s.TryGetGameId("C10", playerId, out gid)).Returns(true);

      object? dummy;
      gameMock.Setup(s => s.MakeMove(gid, It.IsAny<JsonElement>(), out dummy)).Returns(false);

      var clients = new Mock<IHubCallerClients>();
      var groupProxy = SetupGroup(clients, playerId.ToString());

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      var json = JsonDocument.Parse("{}");
      await hub.MakeMove(json.RootElement);

      groupProxy.Verify(p => p.SendCoreAsync("GameUpdate", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
   }
   private static bool HasGameType(object[] args, string expected)
   {
      if (args == null || args.Length != 1 || args[0] == null) return false;
      var arg = args[0]!;
      var prop = arg.GetType().GetProperty("gameType");
      if (prop == null) return false;
      var val = prop.GetValue(arg)?.ToString();
      return val == expected;
   }

   [Fact]
   public async Task StartMatch_NotAllPlayers_ReturnsErrorToCaller()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var ctx = BuildContext(MakeItems("C1"));

      var session = new MatchSession { Code = "C1", GamesList = new List<string> { "TicTacToe" } };
      lobbyMock.Setup(s => s.GetMatchSession("C1")).Returns(session);
      lobbyMock.Setup(s => s.AreAllPlayersInLobby("C1")).Returns(false);

      var (clients, caller) = BuildCallerClients();
      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartMatch();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Not all players have returned to the lobby yet."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartMatch_InGame_ReturnsError()
   {
      var (hub, lobbyMock, _) = CreateHub();
      var ctx = BuildContext(MakeItems("C2"));

      var session = new MatchSession { Code = "C2", InGame = true, GamesList = new List<string> { "TicTacToe" } };
      lobbyMock.Setup(s => s.GetMatchSession("C2")).Returns(session);
      lobbyMock.Setup(s => s.AreAllPlayersInLobby("C2")).Returns(true);

      var (clients, caller) = BuildCallerClients();
      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartMatch();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "A game is still in progress."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartMatch_RoundsEnded_SendsGroupRoundsEnded()
   {
      var (hub, lobbyMock, _) = CreateHub();
      var ctx = BuildContext(MakeItems("C3"));

      var session = new MatchSession { Code = "C3", CurrentRound = 1, GamesList = new List<string> { "TicTacToe" } };
      lobbyMock.Setup(s => s.GetMatchSession("C3")).Returns(session);
      lobbyMock.Setup(s => s.AreAllPlayersInLobby("C3")).Returns(true);

      var clients = new Mock<IHubCallerClients>();
      var groupProxy = SetupGroup(clients, "C3");
      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartMatch();

      groupProxy.Verify(p => p.SendCoreAsync("RoundsEnded", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartMatch_HappyPath_StartsGames_AndNotifiesPlayers()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var ctx = new Mock<HubCallerContext>();
      var items = new Dictionary<object, object?>();
      var keys = typeof(MatchHub).GetNestedType("ContextKeys", BindingFlags.NonPublic)!;
      var codeKey = Enum.Parse(keys, "Code");
      items.Add(codeKey!, "C4");
      ctx.Setup(c => c.Items).Returns(items);

      var p1 = TestHelpers.BuildGuest("A");
      var p2 = TestHelpers.BuildGuest("B");
      var session = new MatchSession { Code = "C4", GamesList = new List<string> { "TicTacToe" }, PlayerGroups = new List<List<User>>() };
      lobbyMock.Setup(s => s.GetMatchSession("C4")).Returns(session);
      lobbyMock.Setup(s => s.AreAllPlayersInLobby("C4")).Returns(true);
      lobbyMock.Setup(s => s.GetPlayersInLobby("C4")).Returns(new List<User> { p1, p2 });
      lobbyMock.Setup(s => s.AddGameId("C4", It.IsAny<Guid>(), It.IsAny<string>())).Returns(true);
      lobbyMock.Setup(s => s.ResetRoundEndTracking("C4"));

      gameMock.Setup(s => s.CreatePlayerGroups(It.Is<List<User>>(l => l.Count == 2), 2))
              .Returns((new List<List<User>> { new() { p1, p2 } }, new List<User>()));
      gameMock.Setup(s => s.StartGame(It.IsAny<string>(), "TicTacToe", It.IsAny<List<User>>())).Returns(true);
      var stateObj = new { ok = true } as object;
      gameMock.Setup(s => s.GetGameState(It.IsAny<string>())).Returns(stateObj);

      var clients = new Mock<IHubCallerClients>();
      var p1Proxy = new Mock<ISingleClientProxy>();
      var p2Proxy = new Mock<ISingleClientProxy>();
      clients.Setup(c => c.Group(p1.Id.ToString())).Returns(p1Proxy.Object);
      clients.Setup(c => c.Group(p2.Id.ToString())).Returns(p2Proxy.Object);
      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartMatch();

      Assert.True(session.InGame);
      Assert.Equal(1, session.CurrentRound);
      Assert.Equal("TicTacToe", session.GameType);
      Assert.Single(session.PlayerGroups);

      p1Proxy.Verify(p => p.SendCoreAsync("MatchStarted",
         It.Is<object[]>(a => HasGameType(a, "TicTacToe")),
         It.IsAny<CancellationToken>()), Times.Once);
      p2Proxy.Verify(p => p.SendCoreAsync("MatchStarted", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartMatch_StartGameFails_RollsBackAndErrors()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var ctx = BuildContext(MakeItems("C5"));

      var a = TestHelpers.BuildGuest("A");
      var b = TestHelpers.BuildGuest("B");
      var c = TestHelpers.BuildGuest("C");
      var d = TestHelpers.BuildGuest("D");
      var session = new MatchSession { Code = "C5", GamesList = new List<string> { "TicTacToe" } };
      lobbyMock.Setup(s => s.GetMatchSession("C5")).Returns(session);
      lobbyMock.Setup(s => s.AreAllPlayersInLobby("C5")).Returns(true);
      lobbyMock.Setup(s => s.GetPlayersInLobby("C5")).Returns(new List<User> { a, b, c, d });

      var groups = new List<List<User>> { new() { a, b }, new() { c, d } };
      gameMock.Setup(s => s.CreatePlayerGroups(It.IsAny<List<User>>(), 2)).Returns((groups, new List<User>()));
      var seq = new MockSequence();
      gameMock.InSequence(seq).Setup(s => s.StartGame(It.IsAny<string>(), "TicTacToe", groups[0])).Returns(true);
      gameMock.InSequence(seq).Setup(s => s.StartGame(It.IsAny<string>(), "TicTacToe", groups[1])).Returns(false);

      var (clients, caller) = BuildCallerClients();
      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartMatch();

      gameMock.Verify(s => s.RemoveGame(It.Is<string>(id => id.Contains("_G0_"))), Times.Once);
      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Failed to start the game."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartMatch_WithUnmatchedPlayers_SendsNoPairing()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var ctx = BuildContext(MakeItems("C8"));

      var p1 = TestHelpers.BuildGuest("P1");
      var p2 = TestHelpers.BuildGuest("P2");
      var p3 = TestHelpers.BuildGuest("P3");
      var session = new MatchSession { Code = "C8", GamesList = new List<string> { "TicTacToe" } };
      lobbyMock.Setup(s => s.GetMatchSession("C8")).Returns(session);
      lobbyMock.Setup(s => s.AreAllPlayersInLobby("C8")).Returns(true);
      lobbyMock.Setup(s => s.GetPlayersInLobby("C8")).Returns(new List<User> { p1, p2, p3 });

      var groups = new List<List<User>> { new() { p1, p2 } };
      var unmatched = new List<User> { p3 };
      gameMock.Setup(s => s.CreatePlayerGroups(It.IsAny<List<User>>(), 2)).Returns((groups, unmatched));
      gameMock.Setup(s => s.StartGame(It.IsAny<string>(), "TicTacToe", groups[0])).Returns(true);
      gameMock.Setup(s => s.GetGameState(It.IsAny<string>())).Returns(new { state = 1 });

      var clients = new Mock<IHubCallerClients>();
      var unmatchedProxy = SetupGroup(clients, p3.Id.ToString());
      // also needed for match started notifications
      clients.Setup(c => c.Group(p1.Id.ToString())).Returns(new Mock<ISingleClientProxy>().Object);
      clients.Setup(c => c.Group(p2.Id.ToString())).Returns(new Mock<ISingleClientProxy>().Object);

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartMatch();

      unmatchedProxy.Verify(p => p.SendCoreAsync("NoPairing", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task EndGame_AllEnded_SendsRoundEndedAndPlayersUpdated()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var ctx = BuildContext(MakeItems("C6"));

      var clients = new Mock<IHubCallerClients>();
      var groupProxy = SetupGroup(clients, "C6");
      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      lobbyMock.Setup(s => s.AreAllGamesEnded("C6")).Returns(true);
      lobbyMock.Setup(s => s.GetMatchRoundInfo("C6")).Returns(new RoundInfoDto(1, 2));

      await hub.EndGame("C6_G0_R0");

      gameMock.Verify(s => s.RemoveGame("C6_G0_R0"), Times.Once);
      lobbyMock.Verify(s => s.MarkGameAsEnded("C6", "C6_G0_R0"), Times.Once);
      groupProxy.Verify(p => p.SendCoreAsync("PlayersUpdated", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
      groupProxy.Verify(p => p.SendCoreAsync("RoundEnded", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task EndGame_NotAllEnded_SendsGameEndedToGameGroup()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var ctx = BuildContext(MakeItems("C7"));

      var clients = new Mock<IHubCallerClients>();
      var gameGroup = new Mock<ISingleClientProxy>();
      clients.Setup(c => c.Group("GID")).Returns(gameGroup.Object);
      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      lobbyMock.Setup(s => s.AreAllGamesEnded("C7")).Returns(false);

      await hub.EndGame("GID");

      gameMock.Verify(s => s.RemoveGame("GID"), Times.Once);
      lobbyMock.Verify(s => s.MarkGameAsEnded("C7", "GID"), Times.Once);
      gameGroup.Verify(p => p.SendCoreAsync("GameEnded", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task GetPlayers_ReturnsDtosFromLobby()
   {
      var (hub, lobbyMock, _) = CreateHub();
      lobbyMock.Setup(s => s.GetPlayersInLobby("CODE"))
               .Returns(new List<User> { new Guest { Name = "A", Wins = 2 }, new Guest { Name = "B", Wins = 5 } });

      var players = await hub.GetPlayers("CODE");
      Assert.Equal(2, players.Count);
      Assert.Contains(players, p => p.Name == "A" && p.Wins == 2);
      Assert.Contains(players, p => p.Name == "B" && p.Wins == 5);
   }

   [Fact]
   public async Task GetGameState_PassesThrough()
   {
      var (hub, _, gameMock) = CreateHub();
      var state = new { ok = true } as object;
      gameMock.Setup(s => s.GetGameState("G1")).Returns(state);
      var result = await hub.GetGameState("G1");
      Assert.Same(state, result);
   }
   [Fact]
   public async Task MakeMove_NoGameId_SendsErrorToCaller()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var mockContext = BuildContext(MakeItems("CODEX", TestHelpers.BuildGuest("Bob")));

      lobbyMock.Setup(s => s.GetMatchSession("CODEX")).Returns(new MatchSession { Code = "CODEX", PlayerGroups = new List<List<User>>() });
      string? outIdNull = null;
      lobbyMock.Setup(s => s.TryGetGameId("CODEX", It.IsAny<Guid>(), out outIdNull)).Returns(false);

      var (clients, caller) = BuildCallerClients();

      hub.Context = mockContext.Object;
      hub.Clients = clients.Object;

      var jsonDoc = JsonDocument.Parse("{}");
      await hub.MakeMove(jsonDoc.RootElement);

      caller.Verify(p => p.SendCoreAsync("Error",
            It.Is<object[]>(o => o != null && o.Length == 1 && o[0] is string), It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task MakeMove_ValidGame_NotifyAllPlayersInGroup()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var playerId = Guid.NewGuid();
      var user = TestHelpers.BuildGuest("Charlie", playerId);

      var mockContext = BuildContext(MakeItems("GAME1", user));

      var session = new MatchSession
      {
         Code = "GAME1",
         PlayerGroups = new List<List<User>> { new List<User> { user } }
      };
      lobbyMock.Setup(s => s.GetMatchSession("GAME1")).Returns(session);

      var gameIdOut = "GAME1_G0_R0";
      lobbyMock.Setup(s => s.TryGetGameId("GAME1", playerId, out gameIdOut)).Returns(true);

      var newState = new { status = "ok" } as object;
      gameMock.Setup(s => s.MakeMove(It.IsAny<string>(), It.IsAny<JsonElement>(), out It.Ref<object?>.IsAny))
         .Returns((string id, JsonElement me, out object? state) => { state = newState; return true; });

      var clients = new Mock<IHubCallerClients>();
      var groupProxy = SetupGroup(clients, playerId.ToString());

      hub.Context = mockContext.Object;
      hub.Clients = clients.Object;

      var jsonDoc = JsonDocument.Parse("{}");
      await hub.MakeMove(jsonDoc.RootElement);

      groupProxy.Verify(p => p.SendCoreAsync("GameUpdate", It.Is<object[]>(arr => arr.Length == 1), It.IsAny<CancellationToken>()), Times.Once);
   }
}


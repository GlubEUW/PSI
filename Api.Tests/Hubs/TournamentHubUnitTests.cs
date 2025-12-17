using System.Reflection;
using System.Text.Json;

using Microsoft.AspNetCore.SignalR;

using Moq;

using Api.Hubs;
using Api.Services;
using Api.Entities;
using Api.Models;
using Api.GameLogic;

namespace Api.Tests.Hubs;

public class TournamentHubUnitTests
{
   private static (TournamentHub hub, Mock<ITournamentService> tournament, Mock<ILobbyService> lobby, Mock<IGameService> game, Mock<IUserService> userSvc, Mock<ICurrentUserAccessor> currentUser) CreateHub()
   {
      var tournamentMock = new Mock<ITournamentService>();
      var lobbyMock = new Mock<ILobbyService>();
      var gameMock = new Mock<IGameService>();
      var userSvc = new Mock<IUserService>();
      var currentUser = new Mock<ICurrentUserAccessor>();

      var hub = new TournamentHub(tournamentMock.Object, lobbyMock.Object, gameMock.Object, userSvc.Object, currentUser.Object);
      return (hub, tournamentMock, lobbyMock, gameMock, userSvc, currentUser);
   }

   private static (object Code, object User) GetContextKeys()
   {
      var t = typeof(TournamentHub).GetNestedType("ContextKeys", BindingFlags.NonPublic)!;
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

   private static string GetGameTypeValue(object payload)
   {
      PropertyInfo prop = payload.GetType().GetProperty("gameType", BindingFlags.Public | BindingFlags.Instance) ?? throw new InvalidOperationException();
      return (string?)prop.GetValue(payload) ?? string.Empty;
   }

   [Fact]
   public async Task GetPlayers_ReturnsDtosFromLobby()
   {
      var (hub, _, lobbyMock, _, _, _) = CreateHub();
      lobbyMock.Setup(s => s.GetPlayersInLobbyDTOs("CODE"))
               .ReturnsAsync(new List<PlayerInfoDto> { new("A", 2), new("B", 5) });

      var players = await hub.GetPlayers("CODE");
      Assert.Equal(2, players.Count);
      Assert.Contains(players, p => p.Name == "A" && p.Wins == 2);
      Assert.Contains(players, p => p.Name == "B" && p.Wins == 5);
   }

   [Fact]
   public async Task StartTournament_AlreadyStarted_SendsError()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");

      var items = MakeItems("CODE1", user);
      var ctx = BuildContext(items);

      var session = new TournamentSession
      {
         Code = "CODE1",
         TournamentId = Guid.NewGuid(),
         NumberOfRounds = 1,
         CurrentRound = 0,
         TournamentStarted = true
      };

      tournamentMock.Setup(s => s.GetTournamentSession("CODE1")).Returns(session);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartTournament();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "The tournament has already started."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartTournament_WhenServiceReturnsError_SendsError()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");

      var items = MakeItems("CODE1", user);
      var ctx = BuildContext(items);

      var session = new TournamentSession
      {
         Code = "CODE1",
         TournamentId = Guid.NewGuid(),
         NumberOfRounds = 1,
         CurrentRound = 0,
         TournamentStarted = false
      };

      tournamentMock.Setup(s => s.GetTournamentSession("CODE1")).Returns(session);
      tournamentMock.Setup(s => s.StartTournament("CODE1")).Returns("boom");

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartTournament();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "boom"),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartTournament_WhenServiceSucceeds_DoesNotSendError()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");

      var items = MakeItems("CODE1", user);
      var ctx = BuildContext(items);

      var session = new TournamentSession
      {
         Code = "CODE1",
         TournamentId = Guid.NewGuid(),
         NumberOfRounds = 1,
         CurrentRound = 0,
         TournamentStarted = false
      };

      tournamentMock.Setup(s => s.GetTournamentSession("CODE1")).Returns(session);
      tournamentMock.Setup(s => s.StartTournament("CODE1")).Returns((string?)null);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartTournament();

      caller.Verify(p => p.SendCoreAsync("Error", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
      tournamentMock.Verify(s => s.StartTournament("CODE1"), Times.Once);
   }

   [Fact]
   public async Task StartRound_RoundAlreadyStarted_SendsError()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");

      var items = MakeItems("CODE2", user);
      var ctx = BuildContext(items);

      tournamentMock.Setup(s => s.TournamentStarted("CODE2")).Returns(true);
      tournamentMock.Setup(s => s.RoundStarted("CODE2")).Returns(true);
      tournamentMock.Setup(s => s.AreAllGamesEnded("CODE2")).Returns(false);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartRound();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Round is still in progress. Wait for all games to finish."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartRound_NotAllGamesEnded_SendsError()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");

      var items = MakeItems("CODE3", user);
      var ctx = BuildContext(items);

      tournamentMock.Setup(s => s.TournamentStarted("CODE3")).Returns(true);
      tournamentMock.Setup(s => s.RoundStarted("CODE3")).Returns(false);
      tournamentMock.Setup(s => s.AreAllGamesEnded("CODE3")).Returns(false);
      tournamentMock.Setup(s => s.StartNextRound("CODE3")).Returns("Not all games have ended.");

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartRound();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Not all games have ended."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartRound_Success_LogsRoundStart_NotifiesPlayersAndGamesStarted()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");
      var items = MakeItems("CODE5", user);
      var ctx = BuildContext(items);

      tournamentMock.Setup(s => s.TournamentStarted("CODE5")).Returns(true);
      tournamentMock.Setup(s => s.RoundStarted("CODE5")).Returns(false);
      tournamentMock.Setup(s => s.StartNextRound("CODE5")).Returns((string?)null);
      tournamentMock.Setup(s => s.LogRoundStartAsync("CODE5")).Returns(Task.CompletedTask);

      var roundInfo = new RoundInfoDto(1, 2);
      tournamentMock.Setup(s => s.GetTournamentRoundInfo("CODE5")).Returns(roundInfo);

      var playerA = TestHelpers.BuildGuest("A");
      var playerB = TestHelpers.BuildGuest("B");

      var game = new Mock<IGame>();
      game.SetupGet(g => g.Players).Returns(new List<User> { playerA, playerB });
      game.SetupGet(g => g.GameType).Returns(Api.Enums.GameType.RockPaperScissors);

      var games = new System.Collections.Concurrent.ConcurrentDictionary<User, IGame>();
      games.TryAdd(playerA, game.Object);
      tournamentMock.Setup(s => s.getGameListForCurrentRound("CODE5")).Returns(games);

      var (clients, _) = BuildCallerClients();
      var lobbyProxy = SetupGroup(clients, "CODE5");
      var aProxy = SetupGroup(clients, playerA.Id.ToString());
      var bProxy = SetupGroup(clients, playerB.Id.ToString());

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartRound();

      tournamentMock.Verify(s => s.LogRoundStartAsync("CODE5"), Times.Once);

      lobbyProxy.Verify(p => p.SendCoreAsync("PlayersUpdated",
         It.Is<object[]>(o => o.Length == 1 && ReferenceEquals(o[0], roundInfo)),
         It.IsAny<CancellationToken>()), Times.Once);

      aProxy.Verify(p => p.SendCoreAsync("GameStarted",
         It.Is<object[]>(o => o.Length == 1 && GetGameTypeValue(o[0]) == "RockPaperScissors"),
         It.IsAny<CancellationToken>()), Times.Once);

      bProxy.Verify(p => p.SendCoreAsync("GameStarted",
         It.Is<object[]>(o => o.Length == 1 && GetGameTypeValue(o[0]) == "RockPaperScissors"),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task MakeMove_GameNotFound_SendsError()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");

      var items = MakeItems("CODE4", user);
      var ctx = BuildContext(items);

      var session = new TournamentSession
      {
         Code = "CODE4",
         TournamentId = Guid.NewGuid(),
         NumberOfRounds = 1,
         CurrentRound = 0,
         TournamentStarted = true
      };

      tournamentMock.Setup(s => s.GetTournamentSession("CODE4")).Returns(session);
      tournamentMock.Setup(s => s.GetGame("CODE4", user, out It.Ref<IGame>.IsAny)).Returns(false);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      var json = JsonDocument.Parse("{}");
      await hub.MakeMove(json.RootElement);

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "An unexpected error occurred"),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task MakeMove_InvalidMove_SendsSpecificError()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var user = TestHelpers.BuildGuest("U");
      var items = MakeItems("CODE6", user);
      var ctx = BuildContext(items);

      var game = new Mock<IGame>();
      game.Setup(g => g.MakeMove(It.IsAny<JsonElement>(), user)).Throws(new Api.Exceptions.InvalidMoveException("bad", user.Id));
      game.SetupGet(g => g.Players).Returns(new List<User> { user });

      IGame? outGame = game.Object;
      tournamentMock.Setup(s => s.GetGame("CODE6", user, out outGame)).Returns(true);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      using var json = JsonDocument.Parse("{}");
      await hub.MakeMove(json.RootElement);

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && ((string)o[0]).Contains("Invalid move")),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task MakeMove_Success_SendsUpdatesAndPersistsIfRoundEnded()
   {
      var (hub, tournamentMock, _, _, _, _) = CreateHub();

      var playerA = TestHelpers.BuildGuest("A");
      var playerB = TestHelpers.BuildGuest("B");

      var items = MakeItems("CODE7", playerA);
      var ctx = BuildContext(items);

      var game = new Mock<IGame>();
      game.SetupGet(g => g.Players).Returns(new List<User> { playerA, playerB });
      game.Setup(g => g.GetState()).Returns(new { ok = true });
      game.Setup(g => g.MakeMove(It.IsAny<JsonElement>(), playerA)).Returns(true);

      IGame? outGame = game.Object;
      tournamentMock.Setup(s => s.GetGame("CODE7", playerA, out outGame)).Returns(true);
      tournamentMock.Setup(s => s.CheckAndSaveResultsIfAllGamesEndedAsync("CODE7")).Returns(Task.CompletedTask);

      var roundInfo = new RoundInfoDto(1, 1);
      tournamentMock.Setup(s => s.GetTournamentRoundInfo("CODE7")).Returns(roundInfo);

      var (clients, _) = BuildCallerClients();
      var aProxy = SetupGroup(clients, playerA.Id.ToString());
      var bProxy = SetupGroup(clients, playerB.Id.ToString());
      var lobbyProxy = SetupGroup(clients, "CODE7");

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      using var json = JsonDocument.Parse("{}");
      await hub.MakeMove(json.RootElement);

      aProxy.Verify(p => p.SendCoreAsync("GameUpdate", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
      bProxy.Verify(p => p.SendCoreAsync("GameUpdate", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
      tournamentMock.Verify(s => s.CheckAndSaveResultsIfAllGamesEndedAsync("CODE7"), Times.Once);

      lobbyProxy.Verify(p => p.SendCoreAsync("PlayersUpdated",
         It.Is<object[]>(o => o.Length == 1 && ReferenceEquals(o[0], roundInfo)),
         It.IsAny<CancellationToken>()), Times.Once);
   }
}


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

   [Fact]
   public async Task GetPlayers_ReturnsDtosFromLobby()
   {
      var (hub, _, lobbyMock, _, _, _) = CreateHub();
      lobbyMock.Setup(s => s.GetPlayersInLobbyDTOs("CODE"))
               .Returns(new List<PlayerInfoDto> { new("A", 2), new("B", 5) });

      var players = await hub.GetPlayers("CODE");
      Assert.Equal(2, players.Count);
      Assert.Contains(players, p => p.Name == "A" && p.Wins == 2);
      Assert.Contains(players, p => p.Name == "B" && p.Wins == 5);
   }

   [Fact]
   public async Task StartTournament_AlreadyStarted_SendsError()
   {
      var (hub, tournamentMock, _, _, _, currentUser) = CreateHub();

      var user = TestHelpers.BuildGuest("U");
      currentUser.Setup(c => c.GetCurrentUser(It.IsAny<HubCallerContext>())).Returns(user);

      var items = MakeItems("CODE1", user);
      var ctx = BuildContext(items);

      var session = new TournamentSession
      {
         Code = "CODE1",
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
   public async Task StartRound_RoundAlreadyStarted_SendsError()
   {
      var (hub, tournamentMock, _, _, _, currentUser) = CreateHub();

      var user = TestHelpers.BuildGuest("U");
      currentUser.Setup(c => c.GetCurrentUser(It.IsAny<HubCallerContext>())).Returns(user);

      var items = MakeItems("CODE2", user);
      var ctx = BuildContext(items);

      tournamentMock.Setup(s => s.RoundStarted("CODE2")).Returns(true);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartRound();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Round has already been started."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task StartRound_NotAllGamesEnded_SendsError()
   {
      var (hub, tournamentMock, _, _, _, currentUser) = CreateHub();

      var user = TestHelpers.BuildGuest("U");
      currentUser.Setup(c => c.GetCurrentUser(It.IsAny<HubCallerContext>())).Returns(user);

      var items = MakeItems("CODE3", user);
      var ctx = BuildContext(items);

      tournamentMock.Setup(s => s.RoundStarted("CODE3")).Returns(false);
      tournamentMock.Setup(s => s.AreAllGamesEnded("CODE3")).Returns(false);

      var (clients, caller) = BuildCallerClients();

      hub.Context = ctx.Object;
      hub.Clients = clients.Object;

      await hub.StartRound();

      caller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Not all games have ended."),
         It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task MakeMove_GameNotFound_SendsError()
   {
      var (hub, tournamentMock, _, _, _, currentUser) = CreateHub();

      var user = TestHelpers.BuildGuest("U");
      currentUser.Setup(c => c.GetCurrentUser(It.IsAny<HubCallerContext>())).Returns(user);

      var items = MakeItems("CODE4", user);
      var ctx = BuildContext(items);

      var session = new TournamentSession
      {
         Code = "CODE4",
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
         It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Player not found in game"),
         It.IsAny<CancellationToken>()), Times.Once);
   }
}


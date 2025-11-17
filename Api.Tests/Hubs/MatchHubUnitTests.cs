using System.Security.Claims;
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

   [Fact]
   public async Task MakeMove_NoGameId_SendsErrorToCaller()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var mockContext = new Mock<HubCallerContext>();
      IDictionary<object, object?> itemsMap = new Dictionary<object, object?>();
      var contextKeysType = typeof(MatchHub).GetNestedType("ContextKeys", BindingFlags.NonPublic);
      var codeKey = Enum.Parse(contextKeysType!, "Code");
      var userKey = Enum.Parse(contextKeysType!, "User");
      itemsMap.Add(codeKey!, "CODEX");
      itemsMap.Add(userKey!, TestHelpers.BuildGuest("Bob"));
      mockContext.Setup(c => c.Items).Returns(itemsMap);
      mockContext.Setup(c => c.ConnectionId).Returns("conn-move");

      lobbyMock.Setup(s => s.GetMatchSession("CODEX")).Returns(new MatchSession { Code = "CODEX", PlayerGroups = new List<List<User>>() });
      string? outIdNull = null;
      lobbyMock.Setup(s => s.TryGetGameId("CODEX", It.IsAny<Guid>(), out outIdNull)).Returns(false);

      var mockClients = new Mock<IHubCallerClients>();
      var mockCaller = new Mock<ISingleClientProxy>();
      mockClients.Setup(c => c.Caller).Returns(mockCaller.Object);

      hub.Context = mockContext.Object;
      hub.Clients = mockClients.Object;

      var jsonDoc = JsonDocument.Parse("{}");
      await hub.MakeMove(jsonDoc.RootElement);

      mockCaller.Verify(p => p.SendCoreAsync("Error",
         It.Is<object[]>(o => o != null && o.Length == 1 && o[0] is string), It.IsAny<CancellationToken>()), Times.Once);
   }

   [Fact]
   public async Task MakeMove_ValidGame_NotifyAllPlayersInGroup()
   {
      var (hub, lobbyMock, gameMock) = CreateHub();
      var playerId = Guid.NewGuid();
      var user = TestHelpers.BuildGuest("Charlie", playerId);

      IDictionary<object, object?> items = new Dictionary<object, object?>();
      var contextKeysType2 = typeof(MatchHub).GetNestedType("ContextKeys", BindingFlags.NonPublic);
      var codeKey2 = Enum.Parse(contextKeysType2!, "Code");
      var userKey2 = Enum.Parse(contextKeysType2!, "User");
      items.Add(codeKey2!, "GAME1");
      items.Add(userKey2!, user);

      var mockContext = new Mock<HubCallerContext>();
      mockContext.Setup(c => c.Items).Returns(items);
      mockContext.Setup(c => c.ConnectionId).Returns("conn-move2");
      mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal());

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

      var mockClients = new Mock<IHubCallerClients>();
      var mockGroupProxy = new Mock<ISingleClientProxy>();
      mockClients.Setup(c => c.Group(playerId.ToString())).Returns(mockGroupProxy.Object);

      hub.Context = mockContext.Object;
      hub.Clients = mockClients.Object;

      var jsonDoc = JsonDocument.Parse("{}");
      await hub.MakeMove(jsonDoc.RootElement);

      mockGroupProxy.Verify(p => p.SendCoreAsync("GameUpdate", It.Is<object[]>(arr => arr.Length == 1), It.IsAny<CancellationToken>()), Times.Once);
   }
}


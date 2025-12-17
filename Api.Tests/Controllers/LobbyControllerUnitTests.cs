using System.Security.Claims;

using Api.Controllers;
using Api.Models;
using Api.Services;

using Microsoft.AspNetCore.Mvc;

using Moq;
namespace Api.Tests.Controllers;

public class LobbyControllerUnitTests
{
   private const string _defaultCode = "code123";

   private static ControllerContext AuthenticatedContext(Guid userId, string userName = "tester")
   {
      return TestHelpers.BuildControllerContext(TestHelpers.CreateClaimsPrincipal(userName, "Guest", userId));
   }
   private static ControllerContext NoNameContext()
   {
      return TestHelpers.BuildControllerContext(new ClaimsPrincipal());
   }
   private static ControllerContext UnauthenticatedContext()
   {
      return TestHelpers.BuildControllerContext(TestHelpers.CreateUnauthenticatedPrincipal());
   }

   private static LobbyController CreateController(ILobbyService service, IGameService gameService, ControllerContext ctx)
   {
      return new(service, gameService) { ControllerContext = ctx };
   }

   private static string? ReadResultProp(IActionResult result, string propName)
   {
      switch (result)
      {
         case OkObjectResult ok when ok.Value is not null:
            return ok.Value.GetType().GetProperty(propName)?.GetValue(ok.Value)?.ToString();
         case BadRequestObjectResult bad when bad.Value is not null:
            return bad.Value.GetType().GetProperty(propName)?.GetValue(bad.Value)?.ToString();
         default:
            return null;
      }
   }

   public static IEnumerable<object[]> UnauthorizedContexts()
   {
      yield return new object[] { UnauthenticatedContext() };
      yield return new object[] { NoNameContext() };
   }

   [Theory]
   [MemberData(nameof(UnauthorizedContexts))]
   public void CanJoinLobby_Unauthorized_ReturnsUnauthorized(ControllerContext ctx)
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, ctx);
      var result = controller.CanJoinLobby(_defaultCode);
      Assert.IsType<UnauthorizedResult>(result);
   }

   [Fact]
   public void CanJoinLobby_ReturnsOkWhenNoError()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CanJoinLobby("code123", userId)).Returns((string?)null);

      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(userId));
      var result = controller.CanJoinLobby(_defaultCode);
      Assert.Equal("Can join lobby.", ReadResultProp(result, "Message"));
   }

   [Fact]
   public void CanJoinLobby_ReturnsBadRequestWhenServiceReturnsError()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CanJoinLobby("code123", userId)).Returns("Full");

      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(userId));
      var result = controller.CanJoinLobby(_defaultCode);
      Assert.Equal("Full", ReadResultProp(result, "Message"));
   }

   [Fact]
   public async Task LeaveLobby_Unauthorized_ReturnsUnauthorized()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, UnauthenticatedContext());
      var result = await controller.LeaveLobby(_defaultCode);
      Assert.IsType<UnauthorizedResult>(result);
   }

   [Fact]
   public async Task LeaveLobby_Unauthorized_NoNameReturnsUnauthorized()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, NoNameContext());
      var result = await controller.LeaveLobby(_defaultCode);
      Assert.IsType<UnauthorizedResult>(result);
   }

   [Fact]
   public async Task LeaveLobby_Success_ReturnsOk()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.LeaveLobby("code123", userId)).ReturnsAsync(true);

      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(userId));
      var result = await controller.LeaveLobby(_defaultCode);
      Assert.Contains("Left lobby", ReadResultProp(result, "Message"));
   }

   [Fact]
   public async Task LeaveLobby_Failure_ReturnsBadRequest()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.LeaveLobby("code123", userId)).ReturnsAsync(false);

      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(userId));
      var result = await controller.LeaveLobby(_defaultCode);
      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_InvalidNumberOfRounds_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));
      var dto = new CreateLobbyDto { NumberOfRounds = 0, NumberOfPlayers = 2, RandomGames = true };
      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_InvalidNumberOfPlayers_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));
      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 1, RandomGames = true };
      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_EmptyGamesListWhenNotRandom_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));
      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = false, GamesList = new List<string>() };
      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_NullGamesListWhenNotRandom_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));
      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = false, GamesList = null };
      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_EmptyOrNullGameInGamesList_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));
      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = false, GamesList = new List<string> { "" } };
      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_NotAGameInGamesList_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var game = new Mock<IGameService>();
      game.Setup(g => g.IsValidGameType(It.IsAny<string>())).Returns(false);
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));
      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = false, GamesList = new List<string> { "Game1" } };
      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_ValidRequestWithRandomTrue_ReturnsOkWithCode()
   {
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CreateLobbyWithSettings(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
         .ReturnsAsync("1234");

      var game = new Mock<IGameService>();
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));

      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = true };

      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.Equal("1234", ReadResultProp(result, "Code"));
   }

   [Fact]
   public async Task CreateLobbyWithSettings_ValidRequestWithGamesList_ReturnsOkWithCode()
   {
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CreateLobbyWithSettings(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
         .ReturnsAsync("1234");

      var game = new Mock<IGameService>();
      game.Setup(g => g.IsValidGameType(It.IsAny<string>())).Returns(true);
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));

      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = false, GamesList = new List<string> { "gameType1" } };

      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.Equal("1234", ReadResultProp(result, "Code"));
   }

   [Fact]
   public async Task CreateLobbyWithSettings_ValidRequestWithGamesListMultipleGames_ReturnsOkWithCode()
   {
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CreateLobbyWithSettings(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
         .ReturnsAsync("1234");

      var game = new Mock<IGameService>();
      game.Setup(g => g.IsValidGameType(It.IsAny<string>())).Returns(true);
      var controller = CreateController(mock.Object, game.Object, AuthenticatedContext(Guid.NewGuid()));

      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = false, GamesList = new List<string> { "gameType1", "gameType2" } };

      var result = await controller.CreateLobbyWithSettings(dto);
      Assert.Equal("1234", ReadResultProp(result, "Code"));
   }
}

using System.Security.Claims;

using Api.Controllers;
using Api.Models;
using Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;


namespace Api.Tests.Controllers;

public class LobbyControllerUnitTests
{
   private static ControllerContext AuthenticatedContext(Guid userId, string userName = "tester")
   {
      var claims = new List<Claim>
         {
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
         };

      var identity = new ClaimsIdentity(claims, "Test");
      var user = new ClaimsPrincipal(identity);

      return new ControllerContext
      {
         HttpContext = new DefaultHttpContext { User = user }
      };
   }

   private static ControllerContext UnauthenticatedContext()
   {
      return new ControllerContext
      {
         HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
      };
   }

   [Fact]
   public async Task CreateLobbyWithSettings_InvalidNumberOfRounds_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(Guid.NewGuid())
      };

      var dto = new CreateLobbyDto { NumberOfRounds = 0, NumberOfPlayers = 2, RandomGames = true };

      var result = await controller.CreateLobbyWithSettings(dto);

      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_InvalidNumberOfPlayers_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(Guid.NewGuid())
      };

      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 1, RandomGames = true };

      var result = await controller.CreateLobbyWithSettings(dto);

      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_EmptyGamesListWhenNotRandom_ReturnsBadRequest()
   {
      var mock = new Mock<ILobbyService>();
      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(Guid.NewGuid())
      };

      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = false, GamesList = new List<string>() };

      var result = await controller.CreateLobbyWithSettings(dto);

      Assert.IsType<BadRequestObjectResult>(result);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_ValidRequest_ReturnsOkWithCode()
   {
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CreateLobbyWithSettings(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<List<string>>()))
         .ReturnsAsync("1234");

      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(Guid.NewGuid())
      };

      var dto = new CreateLobbyDto { NumberOfRounds = 1, NumberOfPlayers = 2, RandomGames = true };

      var result = await controller.CreateLobbyWithSettings(dto);

      var ok = Assert.IsType<OkObjectResult>(result);
      var value = ok.Value!;
      var codeProp = value.GetType().GetProperty("Code");
      Assert.NotNull(codeProp);
      Assert.Equal("1234", codeProp.GetValue(value)?.ToString());
   }

   [Fact]
   public void CanJoinMatch_Unauthorized_ReturnsUnauthorized()
   {
      var mock = new Mock<ILobbyService>();
      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = UnauthenticatedContext()
      };

      var result = controller.CanJoinMatch("code123");

      Assert.IsType<UnauthorizedResult>(result);
   }

   [Fact]
   public void CanJoinMatch_ReturnsOkWhenNoError()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CanJoinLobby("code123", userId)).Returns((string?)null);

      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(userId)
      };

      var result = controller.CanJoinMatch("code123");

      var ok = Assert.IsType<OkObjectResult>(result);
      var value = ok.Value!;
      var messageProp = value.GetType().GetProperty("Message");
      Assert.NotNull(messageProp);
      Assert.Equal("Can join match.", messageProp.GetValue(value)?.ToString());
   }

   [Fact]
   public void CanJoinMatch_ReturnsBadRequestWhenServiceReturnsError()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.CanJoinLobby("code123", userId)).Returns("Full");

      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(userId)
      };

      var result = controller.CanJoinMatch("code123");

      var bad = Assert.IsType<BadRequestObjectResult>(result);
      var value = bad.Value!;
      var messageProp = value.GetType().GetProperty("Message");
      Assert.NotNull(messageProp);
      Assert.Equal("Full", messageProp.GetValue(value)?.ToString());
   }

   [Fact]
   public async Task LeaveMatch_Unauthorized_ReturnsUnauthorized()
   {
      var mock = new Mock<ILobbyService>();
      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = UnauthenticatedContext()
      };

      var result = await controller.LeaveMatch("code123");

      Assert.IsType<UnauthorizedResult>(result);
   }

   [Fact]
   public async Task LeaveMatch_Success_ReturnsOk()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.LeaveMatch("code123", userId)).ReturnsAsync(true);

      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(userId)
      };

      var result = await controller.LeaveMatch("code123");

      var ok = Assert.IsType<OkObjectResult>(result);
      var value = ok.Value!;
      var messageProp = value.GetType().GetProperty("Message");
      Assert.NotNull(messageProp);
      Assert.Contains("Left match", messageProp.GetValue(value)?.ToString());
   }

   [Fact]
   public async Task LeaveMatch_Failure_ReturnsBadRequest()
   {
      var userId = Guid.NewGuid();
      var mock = new Mock<ILobbyService>();
      mock.Setup(s => s.LeaveMatch("code123", userId)).ReturnsAsync(false);

      var controller = new LobbyController(mock.Object)
      {
         ControllerContext = AuthenticatedContext(userId)
      };

      var result = await controller.LeaveMatch("code123");

      Assert.IsType<BadRequestObjectResult>(result);
   }
}


using System.Security.Claims;

using Api.Controllers;
using Api.Models;
using Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace Api.Tests.Controllers;

public class UserControllerUnitTests
{
   private static ControllerContext AuthenticatedContext(Guid userId, string userName = "guest")
   {
      var claims = new[]
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
   public void GuestCreate_ReturnsBadRequest_WhenNameMissing()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.GuestCreate(It.IsAny<UserDto>())).Returns((string?)null);

      var controller = new UserController(mockAuth.Object);

      var dto = new UserDto("", Guid.Empty);

      var result = controller.GuestCreate(dto);

      var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
      Assert.Equal("Name is required.", bad.Value);
   }

   [Fact]
   public void GuestCreate_ReturnsOkWithToken_WhenServiceReturnsToken()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.GuestCreate(It.IsAny<UserDto>())).Returns("token-123");

      var controller = new UserController(mockAuth.Object);

      var dto = new UserDto("player", Guid.Empty);

      var result = controller.GuestCreate(dto);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      Assert.Equal("token-123", ok.Value);
   }

   [Fact]
   public void GetGuestInfo_Unauthorized_ReturnsUnauthorized()
   {
      var mockAuth = new Mock<IAuthService>();
      var controller = new UserController(mockAuth.Object)
      {
         ControllerContext = UnauthenticatedContext()
      };

      var result = controller.GetGuestInfo();

      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Fact]
   public void GetGuestInfo_Authorized_ReturnsUserDto()
   {
      var userId = Guid.NewGuid();
      var userName = "guest-user";

      var mockAuth = new Mock<IAuthService>();
      var controller = new UserController(mockAuth.Object)
      {
         ControllerContext = AuthenticatedContext(userId, userName)
      };

      var result = controller.GetGuestInfo();

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var dto = Assert.IsType<UserDto>(ok.Value);
      Assert.Equal(userName, dto.Name);
      Assert.Equal(userId, dto.Id);
   }
}

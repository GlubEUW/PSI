using System.Security.Claims;

using Api.Controllers;
using Api.Models;
using Api.Services;
using Api.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace Api.Tests.Controllers;

public class UserControllerUnitTests
{
   private static UserController CreateController(IAuthService auth, ControllerContext? ctx = null)
   {
      var controller = new UserController(auth);
      if (ctx is not null) controller.ControllerContext = ctx;
      return controller;
   }
   private static ControllerContext NoNameContext()
   {
      var user = new ClaimsPrincipal();
      return new ControllerContext
      {
         HttpContext = new DefaultHttpContext { User = user }
      };
   }
   private static ControllerContext NoNameButIdContext(Guid userId)
   {
      var claims = new[]
      {
         new Claim(ClaimTypes.NameIdentifier, userId.ToString())
      };

      var identity = new ClaimsIdentity(claims, "Test");
      var user = new ClaimsPrincipal(identity);

      return new ControllerContext
      {
         HttpContext = new DefaultHttpContext { User = user }
      };
   }
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

   public static IEnumerable<object[]> UnauthorizedContexts()
   {
      yield return new object[] { UnauthenticatedContext() };
      yield return new object[] { NoNameContext() };
   }

   [Fact]
   public async Task Login_ReturnsBadRequest_WhenInvalidCredentials()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.LoginAsync(It.IsAny<UserDto>())).ReturnsAsync((string?)null);
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("user", Guid.Empty);

      var result = await controller.Login(dto);

      var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
      Assert.Equal("Invalid name or password.", bad.Value);
   }

   [Fact]
   public async Task Login_ReturnsOkWithToken_WhenValid()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.LoginAsync(It.IsAny<UserDto>())).ReturnsAsync("token-xyz");
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("user", Guid.Empty);

      var result = await controller.Login(dto);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      Assert.Equal("token-xyz", ok.Value);
   }

   [Fact]
   public async Task Register_ReturnsBadRequest_WhenNameExists()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.RegisterAsync(It.IsAny<UserDto>())).ReturnsAsync((RegisteredUser?)null);
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("existing", Guid.Empty);

      var result = await controller.Register(dto);

      var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
      Assert.Equal("Name already exists.", bad.Value);
   }

   [Fact]
   public async Task Register_ReturnsOkWithUser_WhenCreated()
   {
      var mockAuth = new Mock<IAuthService>();
      var created = new RegisteredUser { Id = Guid.NewGuid(), Name = "new-user" };
      mockAuth.Setup(a => a.RegisterAsync(It.IsAny<UserDto>())).ReturnsAsync(created);
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("new-user", Guid.Empty);

      var result = await controller.Register(dto);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var returned = Assert.IsType<RegisteredUser>(ok.Value);
      Assert.Equal(created.Id, returned.Id);
      Assert.Equal(created.Name, returned.Name);
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
      var controller = CreateController(mockAuth.Object);

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
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("player", Guid.Empty);

      var result = controller.GuestCreate(dto);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      Assert.Equal("token-123", ok.Value);
   }

   [Theory]
   [MemberData(nameof(UnauthorizedContexts))]
   public void GetGuestInfo_Unauthorized_ReturnsUnauthorized(ControllerContext ctx)
   {
      var mockAuth = new Mock<IAuthService>();
      var controller = CreateController(mockAuth.Object, ctx);
      var result = controller.GetUserInfo();
      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Fact]
   public void GetGuestInfo_NoNameClaim_ReturnsUnauthorized()
   {
      var mockAuth = new Mock<IAuthService>();
      var controller = CreateController(mockAuth.Object, NoNameButIdContext(Guid.NewGuid()));

      var result = controller.GetUserInfo();

      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Fact]
   public void GetGuestInfo_Authorized_ReturnsUserDto()
   {
      var userId = Guid.NewGuid();
      var userName = "guest-user";

      var mockAuth = new Mock<IAuthService>();
      var controller = CreateController(mockAuth.Object, AuthenticatedContext(userId, userName));

      var result = controller.GetUserInfo();

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var dto = Assert.IsType<UserDto>(ok.Value);
      Assert.Equal(userName, dto.Name);
      Assert.Equal(userId, dto.Id);
   }
}

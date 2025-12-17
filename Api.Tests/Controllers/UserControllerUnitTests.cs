using System.Security.Claims;

using Api.Controllers;
using Api.Models;
using Api.Services;
using Api.Entities;

using Microsoft.AspNetCore.Mvc;

using Moq;

namespace Api.Tests.Controllers;

public class UserControllerUnitTests
{
   private static UserController CreateController(IAuthService auth, IUserService? userService = null, ControllerContext? ctx = null)
   {
      var usr = userService ?? new Mock<IUserService>().Object;
      var controller = new UserController(auth, usr);
      if (ctx is not null) controller.ControllerContext = ctx;
      return controller;
   }
   private static ControllerContext NoNameContext()
   {
      return TestHelpers.BuildControllerContext(new ClaimsPrincipal());
   }
   private static ControllerContext NoNameButIdContext(Guid userId)
   {
      return TestHelpers.BuildControllerContext(TestHelpers.CreatePrincipalWithIdOnly(userId));
   }
   private static ControllerContext AuthenticatedContext(Guid userId, string userName = "guest")
   {
      return TestHelpers.BuildControllerContext(TestHelpers.CreateClaimsPrincipal(userName, "Guest", userId));
   }

   private static ControllerContext RegisteredContext(Guid userId, string userName = "registered")
   {
      return TestHelpers.BuildControllerContext(TestHelpers.CreateClaimsPrincipal(userName, "RegisteredUser", userId));
   }

   public static IEnumerable<object[]> UnauthorizedContexts()
   {
      yield return new object[] { UnauthenticatedContext() };
      yield return new object[] { NoNameContext() };
   }

   [Fact]
   public async Task GuestCreate_ReturnsBadRequest_WhenNameMissing()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.GuestCreateAsync(It.IsAny<UserDto>())).ReturnsAsync((string?)null);
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("", Guid.Empty);

      var result = await controller.GuestCreate(dto);

      var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
      Assert.Equal("Name is required.", bad.Value);
   }

   [Fact]
   public async Task GuestCreate_ReturnsOkWithToken_WhenServiceReturnsToken()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.GuestCreateAsync(It.IsAny<UserDto>())).ReturnsAsync("token-123");
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("player", Guid.Empty);

      var result = await controller.GuestCreate(dto);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      Assert.Equal("token-123", ok.Value);
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

      var bad = Assert.IsType<BadRequestObjectResult>(result);
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

      Assert.IsType<OkResult>(result);
   }

   [Fact]
   public async Task Register_Returns500_WhenServiceThrows()
   {
      var mockAuth = new Mock<IAuthService>();
      mockAuth.Setup(a => a.RegisterAsync(It.IsAny<UserDto>())).ThrowsAsync(new Exception("boom"));
      var controller = CreateController(mockAuth.Object);

      var dto = new UserDto("new-user", Guid.Empty);

      var result = await controller.Register(dto);

      var obj = Assert.IsType<ObjectResult>(result);
      Assert.Equal(500, obj.StatusCode);
      Assert.NotNull(obj.Value);
      var error = (string?)obj.Value!.GetType().GetProperty("error")?.GetValue(obj.Value);
      Assert.Equal("boom", error);
   }

   private static ControllerContext UnauthenticatedContext()
   {
      return TestHelpers.BuildControllerContext(TestHelpers.CreateUnauthenticatedPrincipal());
   }


   [Theory]
   [MemberData(nameof(UnauthorizedContexts))]
   public void GetGuestInfo_Unauthorized_ReturnsUnauthorized(ControllerContext ctx)
   {
      var mockAuth = new Mock<IAuthService>();
      var controller = CreateController(mockAuth.Object, null, ctx);
      var result = controller.GetUserInfo();
      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Fact]
   public void GetGuestInfo_NoNameClaim_ReturnsUnauthorized()
   {
      var mockAuth = new Mock<IAuthService>();
      var controller = CreateController(mockAuth.Object, null, NoNameButIdContext(Guid.NewGuid()));

      var result = controller.GetUserInfo();

      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Fact]
   public void GetGuestInfo_Authorized_ReturnsUserDto()
   {
      var userId = Guid.NewGuid();
      var userName = "guest-user";

      var mockAuth = new Mock<IAuthService>();
      var controller = CreateController(mockAuth.Object, null, AuthenticatedContext(userId, userName));

      var result = controller.GetUserInfo();

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var dto = Assert.IsType<UserDto>(ok.Value);
      Assert.Equal(userName, dto.Name);
      Assert.Equal(userId, dto.Id);
   }

   [Fact]
   public void GetGuestInfo_InvalidIdClaim_ReturnsUnauthorized()
   {
      var identity = new ClaimsIdentity(new[]
      {
         new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
         new Claim(ClaimTypes.Name, "user"),
         new Claim(ClaimTypes.Role, "Guest")
      }, "TestAuth");
      var ctx = TestHelpers.BuildControllerContext(new ClaimsPrincipal(identity));

      var mockAuth = new Mock<IAuthService>();
      var controller = CreateController(mockAuth.Object, null, ctx);

      var result = controller.GetUserInfo();

      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Theory]
   [MemberData(nameof(UnauthorizedContexts))]
   public async Task GetGameStats_Unauthorized_ReturnsUnauthorized(ControllerContext ctx)
   {
      var mockAuth = new Mock<IAuthService>();
      var mockUsers = new Mock<IUserService>();
      var controller = CreateController(mockAuth.Object, mockUsers.Object, ctx);

      var result = await controller.GetGameStats();

      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Fact]
   public async Task GetGameStats_Unauthorized_WhenRoleNotRegisteredUser()
   {
      var mockAuth = new Mock<IAuthService>();
      var mockUsers = new Mock<IUserService>();
      var controller = CreateController(mockAuth.Object, mockUsers.Object, AuthenticatedContext(Guid.NewGuid()));

      var result = await controller.GetGameStats();

      Assert.IsType<UnauthorizedResult>(result.Result);
   }

   [Fact]
   public async Task GetGameStats_NotFound_WhenUserDoesNotExist()
   {
      var userId = Guid.NewGuid();
      var mockAuth = new Mock<IAuthService>();
      var mockUsers = new Mock<IUserService>();
      mockUsers.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
      var controller = CreateController(mockAuth.Object, mockUsers.Object, RegisteredContext(userId, "reg"));

      var result = await controller.GetGameStats();

      Assert.IsType<NotFoundResult>(result.Result);
   }

   [Fact]
   public async Task GetGameStats_ReturnsStats_WithNameFromUser()
   {
      var userId = Guid.NewGuid();
      var mockAuth = new Mock<IAuthService>();
      var mockUsers = new Mock<IUserService>();
      var user = new RegisteredUser { Id = userId, Name = "RealName", PasswordHash = "hash" };
      mockUsers.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
      mockUsers.Setup(s => s.GetUserStatsAsync(userId)).ReturnsAsync(new GameStatsDto
      {
         UserId = userId,
         Name = "OldName",
         TotalWins = 2,
         TotalGamesPlayed = 10
      });

      var controller = CreateController(mockAuth.Object, mockUsers.Object, RegisteredContext(userId, "ClaimName"));

      var result = await controller.GetGameStats();

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var dto = Assert.IsType<GameStatsDto>(ok.Value);
      Assert.Equal("RealName", dto.Name);
      Assert.Equal(2, dto.TotalWins);
      Assert.Equal(10, dto.TotalGamesPlayed);
      mockUsers.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
      mockUsers.Verify(s => s.GetUserStatsAsync(userId), Times.Once);
   }
}

using Api.Services;
using Api.Models;

namespace Api.Tests.Services;

public class AuthServiceUnitTests
{

   [Fact]
   public void GuestCreate_ReturnsNull_WhenNameMissing()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var result = svc.GuestCreate(new UserDto(string.Empty, Guid.Empty));
      Assert.Null(result);
   }

   [Fact]
   public void GuestCreate_ReturnsToken_WhenValidName()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var result = svc.GuestCreate(new UserDto("guest", Guid.Empty));
      Assert.False(string.IsNullOrWhiteSpace(result));
   }

   [Fact]
   public async Task Login_ReturnsNull_WhenUserDoesNotExist()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var result = await svc.LoginAsync(new UserDto("nobody", Guid.Empty) { Password = "pw" });
      Assert.Null(result);
   }

   [Fact]
   public async Task Login_ReturnsNull_WhenPasswordIsNull()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var name = "user3"; var password = "Secret123!";
      await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = password });
      var result = await svc.LoginAsync(new UserDto(name, Guid.Empty) { Password = null });
      Assert.Null(result);
   }

   [Fact]
   public async Task Login_ReturnsNull_WhenPasswordInvalid()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var name = "user2"; await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = "correct" });
      var result = await svc.LoginAsync(new UserDto(name, Guid.Empty) { Password = "wrong" });
      Assert.Null(result);
   }

   [Fact]
   public async Task Login_ReturnsToken_WhenCredentialsValid()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var name = "user1"; var password = "P@ssw0rd!";
      await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = password });
      var result = await svc.LoginAsync(new UserDto(name, Guid.Empty) { Password = password });
      Assert.False(string.IsNullOrWhiteSpace(result));
   }

   [Fact]
   public async Task Register_ReturnsNull_WhenNameExists()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var name = "dup"; await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = "x" });
      var result = await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = "y" });
      Assert.Null(result);
   }

   [Fact]
   public async Task Register_ReturnsUser_WhenNew()
   {
      var svc = (AuthService)TestHelpers.CreateAuthService();
      var result = await svc.RegisterAsync(new UserDto("new", Guid.Empty) { Password = "pw" });
      Assert.NotNull(result);
      Assert.Equal("new", result!.Name);
      Assert.NotEqual(Guid.Empty, result.Id);
      Assert.False(string.IsNullOrWhiteSpace(result.PasswordHash));
   }
}

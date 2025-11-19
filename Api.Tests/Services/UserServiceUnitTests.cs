using Api.Services;
using Api.Entities;

namespace Api.Tests.Services;

public class UserServiceUnitTests
{
   [Fact]
   public void CreateUser_GuestRole_ReturnsGuest()
   {
      var id = Guid.NewGuid();
      var svc = new UserService(TestHelpers.BuildInMemoryDbContext());
      var user = svc.CreateUser("g", id, "Guest");
      Assert.IsType<Guest>(user);
      Assert.Equal(id, user.Id);
      Assert.Equal("g", user.Name);
   }

   [Fact]
   public void CreateUser_NonGuestRole_ReturnsRegisteredUser()
   {
      var id = Guid.NewGuid();
      var svc = new UserService(TestHelpers.BuildInMemoryDbContext());
      var user = svc.CreateUser("r", id, "RegisteredUser");
      Assert.IsType<RegisteredUser>(user);
      Assert.Equal(id, user.Id);
      Assert.Equal("r", user.Name);
   }
}

using System.Security.Claims;

using Api.Entities;
using Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

using Moq;

namespace Api.Tests.Services;

public class CurrentUserAccessorUnitTests
{
    [Fact]
    public void GetCurrentUser_ReturnsNull_WhenMissingClaims()
    {
        var accessor = new CurrentUserAccessor();
        ClaimsPrincipal principal = TestHelpers.CreateUnauthenticatedPrincipal();

        User? user = accessor.GetCurrentUser(principal);

        Assert.Null(user);
    }

    [Fact]
    public void GetCurrentUser_ReturnsGuest_WhenRoleGuest()
    {
        var accessor = new CurrentUserAccessor();
        Guid id = Guid.NewGuid();
        ClaimsPrincipal principal = TestHelpers.CreateClaimsPrincipal("G", "Guest", id);

        User? user = accessor.GetCurrentUser(principal);

        Guest guest = Assert.IsType<Guest>(user);
        Assert.Equal(id, guest.Id);
        Assert.Equal("G", guest.Name);
    }

    [Fact]
    public void GetCurrentUser_ReturnsRegisteredUser_WhenRoleNotGuest()
    {
        var accessor = new CurrentUserAccessor();
        Guid id = Guid.NewGuid();
        ClaimsPrincipal principal = TestHelpers.CreateClaimsPrincipal("R", "RegisteredUser", id);

        User? user = accessor.GetCurrentUser(principal);

        RegisteredUser registered = Assert.IsType<RegisteredUser>(user);
        Assert.Equal(id, registered.Id);
        Assert.Equal("R", registered.Name);
        Assert.Equal(string.Empty, registered.PasswordHash);
    }

    [Fact]
    public void GetCurrentUser_Throws_WhenIdIsNotGuid()
    {
        var accessor = new CurrentUserAccessor();
        var identity = new ClaimsIdentity(new[]
        {
         new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
         new Claim(ClaimTypes.Name, "X"),
         new Claim(ClaimTypes.Role, "Guest")
      }, "TestAuth");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        Assert.Throws<FormatException>(() => accessor.GetCurrentUser(principal));
    }

    [Fact]
    public void GetCurrentUser_FromHubCallerContext_ReturnsNull_WhenPrincipalNull()
    {
        var accessor = new Mock<ICurrentUserAccessor>();
        var ctx = new Mock<HubCallerContext>();
        ctx.SetupGet(c => c.User).Returns((ClaimsPrincipal?)null);

        User? user = accessor.Object.GetCurrentUser(ctx.Object);

        Assert.Null(user);
        accessor.Verify(a => a.GetCurrentUser(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public void GetCurrentUser_FromHubCallerContext_DelegatesToAccessor()
    {
        ClaimsPrincipal principal = TestHelpers.CreateClaimsPrincipal("P", "Guest", Guid.NewGuid());
        var expected = new Guest { Id = Guid.NewGuid(), Name = "U" };

        var accessor = new Mock<ICurrentUserAccessor>();
        accessor.Setup(a => a.GetCurrentUser(principal)).Returns(expected);

        var ctx = new Mock<HubCallerContext>();
        ctx.SetupGet(c => c.User).Returns(principal);

        User? user = accessor.Object.GetCurrentUser(ctx.Object);

        Assert.Same(expected, user);
        accessor.Verify(a => a.GetCurrentUser(principal), Times.Once);
    }

    [Fact]
    public void GetCurrentUser_FromHttpContext_ReturnsNull_WhenPrincipalNull()
    {
        var accessor = new Mock<ICurrentUserAccessor>();
        var http = new Mock<HttpContext>();
        http.SetupGet(h => h.User).Returns((ClaimsPrincipal)null!);

        User? user = accessor.Object.GetCurrentUser(http.Object);

        Assert.Null(user);
        accessor.Verify(a => a.GetCurrentUser(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public void GetCurrentUser_FromHttpContext_DelegatesToAccessor()
    {
        ClaimsPrincipal principal = TestHelpers.CreateClaimsPrincipal("P", "Guest", Guid.NewGuid());
        var expected = new Guest { Id = Guid.NewGuid(), Name = "U" };

        var accessor = new Mock<ICurrentUserAccessor>();
        accessor.Setup(a => a.GetCurrentUser(principal)).Returns(expected);

        var http = new DefaultHttpContext { User = principal };

        User? user = accessor.Object.GetCurrentUser(http);

        Assert.Same(expected, user);
        accessor.Verify(a => a.GetCurrentUser(principal), Times.Once);
    }
}

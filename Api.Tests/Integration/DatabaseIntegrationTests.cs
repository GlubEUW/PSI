using Api.Services;
using Api.Models;

namespace Api.Tests.Integration;

public class DatabaseIntegrationTests
{
    [Fact]
    public async Task AuthService_Register_Login_WorksEndToEnd()
    {
        var context = TestHelpers.BuildInMemoryDbContext("RegisterLoginTest");
        var config = TestHelpers.BuildConfiguration();
        var authService = new AuthService(context, config);
        
        var username = "integrationUser";
        var password = "TestPass123!";
        
        var registered = await authService.RegisterAsync(new UserDto(username, Guid.Empty) 
        { 
            Password = password 
        });

        Assert.NotNull(registered);
        Assert.Equal(username, registered!.Name);

        var token = await authService.LoginAsync(new UserDto(username, Guid.Empty) 
        { 
            Password = password 
        });
        
        Assert.False(string.IsNullOrWhiteSpace(token));

        var failedToken = await authService.LoginAsync(new UserDto(username, Guid.Empty) 
        { 
            Password = "WrongPassword" 
        });
        
        Assert.Null(failedToken);
    }
}
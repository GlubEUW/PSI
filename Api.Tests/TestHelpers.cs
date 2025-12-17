using System.Security.Claims;

using Api.Data;
using Api.Entities;
using Api.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Api.Tests;

public static class TestHelpers
{
   public static DatabaseContext BuildInMemoryDbContext(string? dbName = null)
   {
      var name = string.IsNullOrWhiteSpace(dbName) ? $"Tests_{Guid.NewGuid()}" : dbName!;
      var options = new DbContextOptionsBuilder<DatabaseContext>()
         .UseInMemoryDatabase(name)
         .Options;
      return new DatabaseContext(options);
   }

   public static IConfiguration BuildConfiguration(
      string? token = null,
      string issuer = "TestIssuer",
      string audience = "TestAudience",
      int expiryMinutes = 60)
   {
      var defaultToken = token ?? "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

      var settings = new Dictionary<string, string?>
      {
         ["AppSettings:Token"] = defaultToken,
         ["AppSettings:Issuer"] = issuer,
         ["AppSettings:Audience"] = audience,
         ["AppSettings:TokenExpiryMinutes"] = expiryMinutes.ToString()
      };

      return new ConfigurationBuilder()
         .AddInMemoryCollection(settings!)
         .Build();
   }

   public static ClaimsPrincipal CreateClaimsPrincipal(string name, string role = "Guest", Guid? id = null)
   {
      var userId = id ?? Guid.NewGuid();
      var identity = new ClaimsIdentity(new[]
      {
         new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
         new Claim(ClaimTypes.Name, name),
         new Claim(ClaimTypes.Role, role)
      }, authenticationType: "TestAuth");

      return new ClaimsPrincipal(identity);
   }

   public static IAuthService CreateAuthService(DatabaseContext? ctx = null, IConfiguration? config = null)
   {
      var context = ctx ?? BuildInMemoryDbContext();
      var configuration = config ?? BuildConfiguration();
      return new AuthService(context, configuration);
   }

   public static IGameService CreateGameService()
   {

      return new GameService(new Api.Tests.TestDoubles.TestGameFactory());
   }

   public static async Task<(ILobbyService lobby, string code)> CreateLobbyAsync(int numberOfPlayers = 2, int numberOfRounds = 1, bool randomGames = true, List<string>? gamesList = null)
   {
      var store = new TournamentStore();
      var gameFactory = new Api.Tests.TestDoubles.TestGameFactory();
      var lobby = new LobbyService(store, gameFactory);
      var code = await lobby.CreateLobbyWithSettings(numberOfPlayers, numberOfRounds, randomGames, gamesList);
      return (lobby, code);
   }

   public static Guest BuildGuest(string name = "guest", Guid? id = null)
   {
      return new Guest { Name = name, Id = id ?? Guid.NewGuid() };
   }

   public static Microsoft.AspNetCore.Mvc.ControllerContext BuildControllerContext(ClaimsPrincipal principal)
   {
      return new Microsoft.AspNetCore.Mvc.ControllerContext
      {
         HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
      };
   }

   public static ClaimsPrincipal CreateUnauthenticatedPrincipal()
   {
      return new ClaimsPrincipal(new ClaimsIdentity());
   }

   public static ClaimsPrincipal CreatePrincipalWithIdOnly(Guid? id = null)
   {
      var userId = id ?? Guid.NewGuid();
      var identity = new ClaimsIdentity(new[]
      {
         new Claim(ClaimTypes.NameIdentifier, userId.ToString())
      }, "TestAuth");
      return new ClaimsPrincipal(identity);
   }
}

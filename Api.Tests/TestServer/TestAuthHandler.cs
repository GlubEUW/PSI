using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Tests.TestServer;

public class TestAuthHandler(
   IOptionsMonitor<AuthenticationSchemeOptions> options,
   ILoggerFactory logger,
   UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
   public const string SchemeName = "Test";

   protected override Task<AuthenticateResult> HandleAuthenticateAsync()
   {
      var userId = Request.Headers["X-Test-UserId"].FirstOrDefault() ?? Guid.NewGuid().ToString();
      var name = Request.Headers["X-Test-Name"].FirstOrDefault() ?? "TestUser";
      var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "Guest";

      var claims = new[]
      {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };
      var identity = new ClaimsIdentity(claims, SchemeName);
      var principal = new ClaimsPrincipal(identity);
      var ticket = new AuthenticationTicket(principal, SchemeName);
      return Task.FromResult(AuthenticateResult.Success(ticket));
   }
}

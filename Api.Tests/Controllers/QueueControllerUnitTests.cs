using System.Security.Claims;

using Api.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Api.Tests.Controllers;

public class QueueControllerUnitTests
{
   private static readonly string _joined = "Joined";

   private static ControllerContext AuthenticatedContext(Guid userId, string userName = "tester")
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

   private static QueueController CreateController(ControllerContext ctx)
   {
      return new QueueController { ControllerContext = ctx };
   }

   public static IEnumerable<object[]> AnyContexts()
   {
      yield return new object[] { UnauthenticatedContext() };
      yield return new object[] { AuthenticatedContext(Guid.NewGuid()) };
   }

   [Theory]
   [MemberData(nameof(AnyContexts))]
   public void JoinQueue_ReturnsOk_ForAnyContext(ControllerContext ctx)
   {
      var controller = CreateController(ctx);
      var result = controller.JoinQueue();
      var ok = Assert.IsType<OkObjectResult>(result);
      Assert.Equal(_joined, ok.Value as string);
   }
}

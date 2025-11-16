using System;
using System.Security.Claims;

using Api.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace Api.Tests.Controllers;

public class QueueControllerUnitTests
{
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

   [Fact]
   public void JoinQueue_ReturnsOk_WhenUnauthenticated()
   {
      var controller = new QueueController()
      {
         ControllerContext = UnauthenticatedContext()
      };

      var result = controller.JoinQueue();

      var ok = Assert.IsType<OkObjectResult>(result);
      Assert.Equal("Joined", ok.Value as string);
   }

   [Fact]
   public void JoinQueue_ReturnsOk_WhenAuthenticated()
   {
      var controller = new QueueController()
      {
         ControllerContext = AuthenticatedContext(Guid.NewGuid())
      };

      var result = controller.JoinQueue();

      var ok = Assert.IsType<OkObjectResult>(result);
      Assert.Equal("Joined", ok.Value as string);
   }
}

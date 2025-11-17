using Api.Controllers;

using Microsoft.AspNetCore.Mvc;

namespace Api.Tests.Controllers;

public class QueueControllerUnitTests
{
   private static readonly string _joined = "Joined";

   private static ControllerContext AuthenticatedContext(Guid userId, string userName = "tester")
   {
      var principal = TestHelpers.CreateClaimsPrincipal(userName, "Guest", userId);
      return TestHelpers.BuildControllerContext(principal);
   }

   private static ControllerContext UnauthenticatedContext()
   {
      return TestHelpers.BuildControllerContext(TestHelpers.CreateUnauthenticatedPrincipal());
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

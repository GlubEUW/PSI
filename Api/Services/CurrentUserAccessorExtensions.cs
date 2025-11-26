using Api.Entities;

using Microsoft.AspNetCore.SignalR;

namespace Api.Services;

public static class CurrentUserAccessorExtensions
{
   public static User? GetCurrentUser(this ICurrentUserAccessor accessor, HubCallerContext context)
   {
      var principal = context.User;
      if (principal is null)
         return null;
      return accessor.GetCurrentUser(principal);
   }

   public static User? GetCurrentUser(this ICurrentUserAccessor accessor, HttpContext context)
   {
      var principal = context.User;
      if (principal is null)
         return null;
      return accessor.GetCurrentUser(principal);
   }
}

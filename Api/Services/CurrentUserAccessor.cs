using System.Security.Claims;

using Api.Entities;

namespace Api.Services;

public class CurrentUserAccessor : ICurrentUserAccessor
{
   public User? GetCurrentUser(ClaimsPrincipal principal)
   {
      var name = principal.Identity?.Name;
      var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var role = principal.FindFirst(ClaimTypes.Role)?.Value;

      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
         return null;

      if (role == "Guest")
      {
         return new Guest
         {
            Id = Guid.Parse(id),
            Name = name
         };
      }
      else
      {
         return new RegisteredUser
         {
            Id = Guid.Parse(id),
            Name = name,
            PasswordHash = string.Empty
         };
      }
   }
}
using System.Security.Claims;

using Api.Entities;

namespace Api.Services;

public class CurrentUserAccessor(IUserService userService) : ICurrentUserAccessor
{
   private readonly IUserService _userService = userService;

   public User? GetCurrentUser(ClaimsPrincipal principal)
   {
      var name = principal.Identity?.Name;
      var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var role = principal.FindFirst(ClaimTypes.Role)?.Value;

      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
         return null;

      return _userService.CreateUser(name, Guid.Parse(id), role);
   }
}

using System.Security.Claims;

using Api.Entities;

namespace Api.Services;

public interface ICurrentUserAccessor
{
   public User? GetCurrentUser(ClaimsPrincipal principal);
}

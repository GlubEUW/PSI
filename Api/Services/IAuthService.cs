using Api.Models;

namespace Api.Services;

public interface IAuthService
{
   public string? GuestCreate(UserDto request);
}

using Api.Models;

namespace Api.Services;

public interface IAuthService
{
   string? GuestCreate(UserDto request);
}

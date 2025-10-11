using Api.Models;

namespace Api.Services;

public interface IAuthService
{
   string? GuestCreate(GuestDto request);
}

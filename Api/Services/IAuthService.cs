using Api.Entities;
using Api.Models;

namespace Api.Services;

public interface IAuthService
{
   public Task<string?> GuestCreateAsync(UserDto request);
   public Task<string?> LoginAsync(UserDto request);
   public Task<RegisteredUser?> RegisterAsync(UserDto request);
}

using Api.Entities;
using Api.Models;

namespace Api.Services;

public interface IAuthService
{
   public string? GuestCreate(UserDto request);
   public Task<string?> LoginAsync(UserDto request);
   public Task<RegisteredUser?> RegisterAsync(UserDto request);
}

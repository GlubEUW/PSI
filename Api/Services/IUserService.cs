using Api.Entities;
using Api.Models;

namespace Api.Services;

public interface IUserService
{
   public Task<User?> GetUserByIdAsync(Guid id);
   public Task<GameStatsDto> GetUserStatsAsync(Guid userId);
}
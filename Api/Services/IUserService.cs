using Api.Entities;

namespace Api.Services;

public interface IUserService
{
   public User CreateUser(string name, Guid id, string role);
   public Task LoadUserStatsAsync(User user);
   public Task SaveUserStatsAsync(User user);
   public Task<User?> GetUserByIdAsync(Guid id);
}
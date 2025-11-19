using Api.Entities;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class UserService(DatabaseContext context) : IUserService
{
   public User CreateUser(string name, Guid id, string role)
   {
      return (role == "Guest") ?
      new Guest()
      {
         Name = name,
         Id = id
      }
      : new RegisteredUser()
      {
         Name = name,
         Id = id
      };
   }
   public async Task LoadUserStatsAsync(User user)
   {
      if (user is not RegisteredUser)
         return;

      var statsData = await context.GameStats
         .FirstOrDefaultAsync(gs => gs.UserId == user.Id);

      if (statsData is not null)
         user.LoadFromGameStatsDto(statsData);
      else
      {
         var newStats = user.ToGameStatsDto();
         context.GameStats.Add(newStats);
         await context.SaveChangesAsync();
      }
   }
   public async Task SaveUserStatsAsync(User user)
   {
      if (user is not RegisteredUser)
         return;

      var statsData = user.ToGameStatsDto();

      var existing = await context.GameStats
         .FirstOrDefaultAsync(gs => gs.UserId == user.Id);

      if (existing is not null)
      {
         existing.TotalWins = statsData.TotalWins;
         existing.TotalGamesPlayed = statsData.TotalGamesPlayed;
         existing.TicTacToeWins = statsData.TicTacToeWins;
         existing.TicTacToeGamesPlayed = statsData.TicTacToeGamesPlayed;
         existing.RockPaperScissorsWins = statsData.RockPaperScissorsWins;
         existing.RockPaperScissorsGamesPlayed = statsData.RockPaperScissorsGamesPlayed;
         existing.ConnectFourWins = statsData.ConnectFourWins;
         existing.ConnectFourGamesPlayed = statsData.ConnectFourGamesPlayed;
      }
      else
         context.GameStats.Add(statsData);

      await context.SaveChangesAsync();
   }

   public async Task<User?> GetUserByIdAsync(Guid id)
   {
      var user = await context.Users
         .FirstOrDefaultAsync(u => u.Id == id);

      if (user is not null)
      {
         var stats = await context.GameStats
            .FirstOrDefaultAsync(gs => gs.UserId == id);

         if (stats is not null)
            user.LoadFromGameStatsDto(stats);
      }

      return user;
   }
}

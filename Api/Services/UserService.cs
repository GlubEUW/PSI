using Api.Entities;
using Api.Data;
using Api.Models;

using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class UserService(DatabaseContext context) : IUserService
{
   public async Task<User?> GetUserByIdAsync(Guid id)
   {
      return await context.Users.FirstOrDefaultAsync(u => u.Id == id);
   }

   public async Task<GameStatsDto> GetUserStatsAsync(Guid userId)
   {
      var userRounds = await (from ur in context.UserRounds
                              join r in context.Rounds on ur.RoundId equals r.Id
                              where ur.UserId == userId
                              select new
                              {
                                 ur.PlayerPlacement,
                                 r.GameType
                              }).ToListAsync();

      var stats = new GameStatsDto
      {
         UserId = userId,
         TotalGamesPlayed = userRounds.Count,
         TotalWins = userRounds.Count(ur => ur.PlayerPlacement == 1)
      };

      var ticTacToe = userRounds.Where(ur => ur.GameType == "TicTacToe").ToList();
      stats.TicTacToeGamesPlayed = ticTacToe.Count;
      stats.TicTacToeWins = ticTacToe.Count(ur => ur.PlayerPlacement == 1);

      var rps = userRounds.Where(ur => ur.GameType == "RockPaperScissors").ToList();
      stats.RockPaperScissorsGamesPlayed = rps.Count;
      stats.RockPaperScissorsWins = rps.Count(ur => ur.PlayerPlacement == 1);

      var connectFour = userRounds.Where(ur => ur.GameType == "ConnectFour").ToList();
      stats.ConnectFourGamesPlayed = connectFour.Count;
      stats.ConnectFourWins = connectFour.Count(ur => ur.PlayerPlacement == 1);

      return stats;
   }
}
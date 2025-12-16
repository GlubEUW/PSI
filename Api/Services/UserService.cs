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
      var userGames = await (from ur in context.UserRound
                             join g in context.Games on ur.GameId equals g.Id
                             where ur.UserId == userId
                             select new
                             {
                                ur.PlayerPlacement,
                                g.GameType
                             }).ToListAsync();

      var stats = new GameStatsDto
      {
         UserId = userId,
         TotalGamesPlayed = userGames.Count,
         TotalWins = userGames.Count(ur => ur.PlayerPlacement == 1)
      };

      var ticTacToe = userGames.Where(ur => ur.GameType == "TicTacToe").ToList();
      stats.TicTacToeGamesPlayed = ticTacToe.Count;
      stats.TicTacToeWins = ticTacToe.Count(ur => ur.PlayerPlacement == 1);

      var rps = userGames.Where(ur => ur.GameType == "RockPaperScissors").ToList();
      stats.RockPaperScissorsGamesPlayed = rps.Count;
      stats.RockPaperScissorsWins = rps.Count(ur => ur.PlayerPlacement == 1);

      var connectFour = userGames.Where(ur => ur.GameType == "ConnectFour").ToList();
      stats.ConnectFourGamesPlayed = connectFour.Count;
      stats.ConnectFourWins = connectFour.Count(ur => ur.PlayerPlacement == 1);

      return stats;
   }
}
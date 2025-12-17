using Api.Data;
using Api.Models;
using Api.Entities;

using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class LeaderBoardService(DatabaseContext context) : ILeaderBoardService
{
   public async Task<LeaderBoardPageDto> GetLeaderBoardAsync(int page, int pageSize)
   {
      if (page < 0)
         page = 0;

      if (pageSize <= 0)
         pageSize = 100;

      if (pageSize > 100)
         pageSize = 100;

      IQueryable<RegisteredUser> usersQuery = context.Users
         .AsNoTracking()
         .OfType<RegisteredUser>()
         .Where(u => EF.Property<string?>(u, nameof(RegisteredUser.PasswordHash)) != null);

      int totalUsers = await usersQuery.CountAsync();

      IQueryable<LeaderBoardUserDto> query = usersQuery
         .Select(u => new LeaderBoardUserDto
         {
            Id = u.Id,
            Name = u.Name,
            TotalWins = (from ur in context.UserRound
                         where ur.UserId == u.Id && ur.PlayerPlacement == 1
                         select 1).Count(),
            TicTacToeWins = (from ur in context.UserRound
                             join g in context.Games on ur.GameId equals g.Id
                             where ur.UserId == u.Id
                                && ur.PlayerPlacement == 1
                                && g.GameType == "TicTacToe"
                             select 1).Count(),
            RockPaperScissorsWins = (from ur in context.UserRound
                                     join g in context.Games on ur.GameId equals g.Id
                                     where ur.UserId == u.Id
                                        && ur.PlayerPlacement == 1
                                        && g.GameType == "RockPaperScissors"
                                     select 1).Count(),
            ConnectFourWins = (from ur in context.UserRound
                               join g in context.Games on ur.GameId equals g.Id
                               where ur.UserId == u.Id
                                  && ur.PlayerPlacement == 1
                                  && g.GameType == "ConnectFour"
                               select 1).Count()
         })
         .OrderByDescending(u => u.TotalWins)
         .ThenBy(u => u.Name)
         .Skip(page * pageSize)
         .Take(pageSize);

      List<LeaderBoardUserDto> users = await query.ToListAsync();

      return new LeaderBoardPageDto
      {
         Page = page,
         PageSize = pageSize,
         TotalUsers = totalUsers,
         Users = users
      };
   }
}

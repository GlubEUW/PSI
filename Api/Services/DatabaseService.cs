using Api.Data;
using Api.Entities;

using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class DatabaseService(IDbContextFactory<DatabaseContext> contextFactory) : IDatabaseService
{
   private readonly IDbContextFactory<DatabaseContext> _contextFactory = contextFactory;

   public async Task<int> GetUserWinsInTournamentAsync(Guid tournamentId, Guid userId, CancellationToken cancellationToken = default)
   {
      await using DatabaseContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);

      IQueryable<UserGame> query =
         from ur in context.UserRound
         join g in context.Games on ur.GameId equals g.Id
         where ur.UserId == userId
            && g.TournamentId == tournamentId
            && ur.PlayerPlacement == 1
         select ur;

      var wins = await query.CountAsync(cancellationToken);
      return wins;
   }
}

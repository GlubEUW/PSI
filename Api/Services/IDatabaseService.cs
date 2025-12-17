namespace Api.Services;

public interface IDatabaseService
{
   public Task<int> GetUserWinsInTournamentAsync(Guid tournamentId, Guid userId, CancellationToken cancellationToken = default);
}

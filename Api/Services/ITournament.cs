using Api.Entities;

namespace Api.Services;

public interface ITournamentService
{
   bool CreateTournament(string tournamentId, List<string> players, string gameType);

   MatchPairing? GetCurrentMatch(string tournamentId);

   MatchPairing? AdvanceToNextMatch(string tournamentId);

   bool RecordMatchResult(string tournamentId, string? winner, bool isDraw = false);

   Dictionary<string, double> GetScores(string tournamentId);

   List<PlayerScore> GetLeaderboard(string tournamentId);

   TournamentStatus? GetTournamentStatus(string tournamentId);

   bool IsTournamentComplete(string tournamentId);

   bool RemoveTournament(string tournamentId);
}

using System.Collections.Concurrent;
using Api.Entities;

namespace Api.Services;

public class TournamentService : ITournamentService
{
   private static readonly ConcurrentDictionary<string, Tournament> _tournaments = new();

   public bool CreateTournament(string tournamentId, List<string> players, string gameType)
   {
      if (players == null || players.Count < 2)
         return false;

      if (_tournaments.ContainsKey(tournamentId))
         return false;

      var tournament = new Tournament
      {
         Id = tournamentId,
         Players = new List<string>(players),
         GameType = gameType,
         Matches = GenerateRoundRobinMatches(players),
         CurrentMatchIndex = 0,
         Scores = players.ToDictionary(p => p, _ => 0.0),
         IsComplete = false
      };

      return _tournaments.TryAdd(tournamentId, tournament);
   }

   private List<MatchPairing> GenerateRoundRobinMatches(List<string> players)
   {
      return (
          from i in Enumerable.Range(0, players.Count)
          from j in Enumerable.Range(i + 1, players.Count - (i + 1))
          select new MatchPairing
          {
             Player1 = players[i],
             Player2 = players[j],
             IsComplete = false
          }
      ).ToList();
   }


   public MatchPairing? GetCurrentMatch(string tournamentId)
   {
      if (!_tournaments.TryGetValue(tournamentId, out var tournament))
         return null;

      if (tournament.CurrentMatchIndex >= tournament.Matches.Count)
         return null;

      return tournament.Matches[tournament.CurrentMatchIndex];
   }

   public MatchPairing? AdvanceToNextMatch(string tournamentId)
   {
      if (!_tournaments.TryGetValue(tournamentId, out var tournament))
         return null;

      tournament.CurrentMatchIndex++;

      if (tournament.CurrentMatchIndex >= tournament.Matches.Count)
      {
         tournament.IsComplete = true;
         return null;
      }

      return tournament.Matches[tournament.CurrentMatchIndex];
   }

   public bool RecordMatchResult(string tournamentId, string? winner, bool isDraw = false)
   {
      if (!_tournaments.TryGetValue(tournamentId, out var tournament))
         return false;

      if (tournament.CurrentMatchIndex >= tournament.Matches.Count)
         return false;

      var currentMatch = tournament.Matches[tournament.CurrentMatchIndex];

      if (currentMatch.IsComplete)
         return false;

      currentMatch.IsComplete = true;
      currentMatch.IsDraw = isDraw;
      currentMatch.Winner = winner;

      if (isDraw)
      {
         tournament.Scores[currentMatch.Player1] += 0.5;
         tournament.Scores[currentMatch.Player2] += 0.5;
      }
      else if (!string.IsNullOrEmpty(winner))
      {
         if (tournament.Scores.ContainsKey(winner))
         {
            tournament.Scores[winner] += 1.0;
         }
      }

      return true;
   }

   public Dictionary<string, double> GetScores(string tournamentId)
   {
      if (!_tournaments.TryGetValue(tournamentId, out var tournament))
         return new Dictionary<string, double>();

      return new Dictionary<string, double>(tournament.Scores);
   }

   public List<PlayerScore> GetLeaderboard(string tournamentId)
   {
      if (!_tournaments.TryGetValue(tournamentId, out var tournament))
         return new List<PlayerScore>();

      var playerStats = tournament.Players.Select(player =>
      {
         var wins = tournament.Matches
               .Count(m => m.IsComplete && m.Winner == player);

         var draws = tournament.Matches
               .Count(m => m.IsComplete && m.IsDraw &&
                          (m.Player1 == player || m.Player2 == player));

         var totalMatches = tournament.Matches
               .Count(m => m.IsComplete &&
                          (m.Player1 == player || m.Player2 == player));

         var losses = totalMatches - wins - draws;

         return new PlayerScore
         {
            PlayerName = player,
            Score = tournament.Scores[player],
            Wins = wins,
            Draws = draws,
            Losses = losses
         };
      })
      .OrderByDescending(ps => ps.Score)
      .ThenByDescending(ps => ps.Wins)
      .ToList();

      return playerStats;
   }

   public TournamentStatus? GetTournamentStatus(string tournamentId)
   {
      if (!_tournaments.TryGetValue(tournamentId, out var tournament))
         return null;

      return new TournamentStatus
      {
         Id = tournament.Id,
         Players = new List<string>(tournament.Players),
         GameType = tournament.GameType,
         TotalMatches = tournament.Matches.Count,
         CompletedMatches = tournament.Matches.Count(m => m.IsComplete),
         CurrentMatch = GetCurrentMatch(tournamentId),
         Leaderboard = GetLeaderboard(tournamentId),
      };
   }

   public bool IsTournamentComplete(string tournamentId)
   {
      if (!_tournaments.TryGetValue(tournamentId, out var tournament))
         return false;

      return tournament.IsComplete;
   }

   public bool RemoveTournament(string tournamentId)
   {
      var removed = _tournaments.TryRemove(tournamentId, out _);
      if (removed)
         Console.WriteLine($"Tournament {tournamentId} removed.");
      return removed;
   }
}

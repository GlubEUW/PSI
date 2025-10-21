namespace Api.Entities;

public class Tournament
{
   public string Id { get; set; } = string.Empty;
   public List<string> Players { get; set; } = new();
   public string GameType { get; set; } = string.Empty;
   public List<MatchPairing> Matches { get; set; } = new();
   public int CurrentMatchIndex { get; set; } = 0;
   public Dictionary<string, double> Scores { get; set; } = new();
   public bool IsComplete { get; set; } = false;
}

public class MatchPairing
{
   public string Player1 { get; set; } = string.Empty;
   public string Player2 { get; set; } = string.Empty;
   public string? Winner { get; set; }
   public bool IsDraw { get; set; }
   public bool IsComplete { get; set; }
}

public class PlayerScore
{
   public string PlayerName { get; set; } = string.Empty;
   public double Score { get; set; }
   public int Wins { get; set; }
   public int Draws { get; set; }
   public int Losses { get; set; }
}

public class TournamentStatus
{
   public string Id { get; set; } = string.Empty;
   public List<string> Players { get; set; } = new();
   public string GameType { get; set; } = string.Empty;
   public int TotalMatches { get; set; }
   public int CompletedMatches { get; set; }
   public MatchPairing? CurrentMatch { get; set; }
   public List<PlayerScore> Leaderboard { get; set; } = new();
}

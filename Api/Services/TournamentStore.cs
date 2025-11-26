using System.Collections.Concurrent;

using Api.Models;

namespace Api.Services;

public class TournamentStore
{
   public ConcurrentDictionary<string, TournamentSession> Sessions { get; } = new();
}

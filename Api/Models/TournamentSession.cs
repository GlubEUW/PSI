using Api.Entities;
using Api.Enums;
using Api.GameLogic;

using System.Collections.Concurrent;
namespace Api.Models;

public class TournamentSession
{
   public required string Code { get; set; }
   public required int NumberOfRounds { get; set; } = 1;
   public required int CurrentRound { get; set; } = 0;
   public required bool TournamentStarted { get; set; } = false;
   public bool RoundStarted { get; set; }
   public List<User> Players { get; set; } = new List<User>();
   public List<GameType> GameTypesByRounds { get; set; } = new();
   public ConcurrentDictionary<User, IGame> GamesByPlayers = new();
}

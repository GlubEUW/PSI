
using System.Collections.Concurrent;

using Api.Entities;
using Api.GameLogic;
using Api.Models;

namespace Api.Services;

public interface ITournamentService
{
   public RoundInfoDto GetTournamentRoundInfo(string code);
   public bool AddGameId(string code, Guid userId, string gameId = "");
   public bool RemoveGameId(string? code, Guid? userId);
   public TournamentSession? GetTournamentSession(string code);
   public bool AreAllGamesEnded(string code);
   public ConcurrentDictionary<User, IGame> getGameListForCurrentRound(string code);
   public string? StartNextRound(string code);
   public bool HalfPlayersReadyForNextRound(string code);
   public List<User>? getTargetGroup(User user, string code);
   public bool RoundStarted(string code);
   public bool GetGame(string code, User user, out IGame game);
   public bool TournamentStarted(string code);
   public bool StartTournament(string code);
}

using Api.Models;
using Api.Entities;

namespace Api.Services;

public interface ILobbyService
{
   public RoundInfoDto GetMatchRoundInfo(string code);
   public bool AddGameId(string code, Guid userId, string gameId = "");
   public bool RemoveGameId(string? code, Guid? userId);
   public bool TryGetGameId(string? code, Guid userId, out string? gameId);
   public List<User> GetPlayersInLobby(string code);
   public Task<string?> JoinMatch(string code, User user);
   public Task<bool> LeaveMatch(string code, Guid userId);
   public Task<string> CreateLobbyWithSettings(int numberOfPlayers, int numberOfRounds, bool randomGames, List<string>? gamesList);
   public string? CanJoinLobby(string code, Guid userId);
   public TournamentSession? GetTournamentSession(string code);
   public void MarkGameAsEnded(string code, string gameId);
   public bool AreAllGamesEnded(string code);
   public void ResetRoundEndTracking(string code);
   public bool AreAllPlayersInLobby(string code);
}

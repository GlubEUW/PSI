using Api.Models;
using Api.Entities;

namespace Api.Services;

public interface ITournamentService
{
   public RoundInfoDto GetTournamentRoundInfo(string code);
   public bool AddGameId(string code, Guid userId, string gameId = "");
   public bool RemoveGameId(string? code, Guid? userId);
   public bool TryGetGameId(string? code, Guid userId, out string? gameId);
   public List<User> GetPlayersInLobby(string code);
   public Task<string?> JoinLobby(string code, User user);
   public Task<bool> LeaveLobby(string code, Guid userId);
   public Task<string> CreateLobbyWithSettings(int numberOfPlayers, int numberOfRounds, bool randomGames, List<string>? gamesList);
   public string? CanJoinLobby(string code, Guid userId);
   public TournamentSession? GetTournamentSession(string code);
   public void MarkGameAsEnded(string code, string gameId);
   public bool AreAllGamesEnded(string code);
   public void ResetRoundEndTracking(string code);
   public bool AreAllPlayersInLobby(string code);
}

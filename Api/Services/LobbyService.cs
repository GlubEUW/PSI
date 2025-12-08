using Api.Models;
using Api.Entities;
using Api.GameLogic;
using Api.Enums;

namespace Api.Services;

public class LobbyService(TournamentStore tournamentStore, IGameFactory gameFactory) : ILobbyService
{
   private readonly TournamentStore _store = tournamentStore;
   private readonly IGameFactory _gameFactory;

   public List<User> GetPlayersInLobby(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session) && session is not null)
         return session.Players;

      return new List<User>();
   }

   public Task<string?> JoinLobby(string code, User user)
   {
      if (!_store.Sessions.TryGetValue(code, out var session))
         return Task.FromResult<string?>("Game does not exist.");

      var error = CanJoinLobby(code, user.Id);
      if (error is null)
         session.Players.Add(user);

      return Task.FromResult(error);
   }

   public Task<bool> LeaveLobby(string code, Guid userId)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
      {
         var user = session.Players.FirstOrDefault(u => u.Id == userId);
         if (user is not null)
            session.Players.Remove(user);

         if (session.Players.Count == 0)
            return Task.FromResult(_store.Sessions.TryRemove(code, out _));

         return Task.FromResult(true);
      }
      return Task.FromResult(false);
   }

   public string? CanJoinLobby(string code, Guid userId)
   {
      if (_store.Sessions.TryGetValue(code, out var session) && session != null)
      {
         if (session.TournamentStarted)
            return "Game already started.";

         if (session.Players.Count >= session.Players.Capacity)
            return "Lobby is full.";

         if (session.Players.Any(u => u.Id == userId))
            return "Name already taken.";

         return null;
      }
      return "Game does not exist.";
   }

   public Task<string> CreateLobbyWithSettings(int numberOfPlayers, int numberOfRounds, bool randomGames, List<string>? gamesTypesListInString)
   {
      string code;
      do
      {
         code = GenerateUniqueLobbyCode();
      } while (_store.Sessions.ContainsKey(code));

      List<GameType> finalGamesTypes;

      if (randomGames || gamesTypesListInString == null || gamesTypesListInString.Count == 0)
         finalGamesTypes = GenerateRandomGameTypesList(numberOfRounds);

      else
         finalGamesTypes = gamesTypesListInString
            .Select(name => Enum.Parse<GameType>(name, ignoreCase: true))
            .ToList();



      _store.Sessions[code] = new TournamentSession
      {
         Code = code,
         Players = new List<User>(numberOfPlayers),
         GameTypesByRounds = finalGamesTypes,
         NumberOfRounds = numberOfRounds,
         TournamentStarted = false,
         CurrentRound = 0
      };
      return Task.FromResult(code);
   }

   public TournamentSession? GetTournamentSession(string code)
   {
      if (_store.Sessions.TryGetValue(code, out var session))
         return session;

      return null;
   }

   private List<GameType> GenerateRandomGameTypesList(int count)
   {
      var availableGames = _gameFactory.ValidGameTypes.ToArray();
      var random = new Random();

      return Enumerable.Range(0, count)
         .Select(_ => availableGames[random.Next(availableGames.Length)])
         .ToList();
   }

   private string GenerateUniqueLobbyCode()
   {
      var random = new Random();
      string code;

      do
      {
         code = random.Next(1000, 9999).ToString();
      }
      while (_store.Sessions.ContainsKey(code));

      return code;
   }
}

using Api.Entities;
using Api.Enums;
using Api.Models;
using Api.Services;
using Api.Tests.TestDoubles;
using Api.Data;

namespace Api.Tests.Services;

public class LobbyServiceUnitTests
{
   private static LobbyService CreateService()
   {
      var store = new TournamentStore();
      var factory = new TestGameFactory();
      var contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"LobbyService_{Guid.NewGuid()}");
      return new LobbyService(store, factory, contextFactory);
   }

   [Fact]
   public async Task JoinLobby_ReturnsError_WhenSessionMissing()
   {
      var svc = CreateService();
      var user = new Guest { Id = Guid.NewGuid(), Name = "x" };
      var err = await svc.JoinLobby("missing", user);
      Assert.Equal("Game does not exist.", err);
   }

   [Fact]
   public async Task JoinLobby_AddsPlayer_WhenAllowed()
   {
      var svc = CreateService();
      var code = await svc.CreateLobbyWithSettings(2, 1, true, null);
      var user = new Guest { Id = Guid.NewGuid(), Name = "p" };

      var err = await svc.JoinLobby(code, user);
      Assert.Null(err);
      Assert.Contains(svc.GetPlayersInLobby(code), u => u.Id == user.Id);
   }

   [Fact]
   public async Task LeaveLobby_RemovesPlayer_AndRemovesSessionWhenEmpty()
   {
      var svc = CreateService();
      var code = await svc.CreateLobbyWithSettings(1, 1, true, null);
      var user = new Guest { Id = Guid.NewGuid(), Name = "p" };
      await svc.JoinLobby(code, user);

      var ok = await svc.LeaveLobby(code, user.Id);
      Assert.True(ok);
      Assert.Empty(svc.GetPlayersInLobby(code));
      Assert.Null(svc.GetTournamentSession(code));
   }

   [Fact]
   public async Task CanJoinLobby_ReturnsLobbyIsFull_WhenCapacityReached()
   {
      var svc = CreateService();
      var code = await svc.CreateLobbyWithSettings(1, 1, true, null);
      var p1 = new Guest { Id = Guid.NewGuid(), Name = "p1" };
      await svc.JoinLobby(code, p1);

      var msg = svc.CanJoinLobby(code, Guid.NewGuid());
      Assert.Equal("Lobby is full.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_ReturnsAlreadyStarted_WhenTournamentStarted()
   {
      var svc = CreateService();
      var code = await svc.CreateLobbyWithSettings(2, 1, true, null);
      var session = svc.GetTournamentSession(code)!;
      session.TournamentStarted = true;

      var msg = svc.CanJoinLobby(code, Guid.NewGuid());
      Assert.Equal("Game already started.", msg);
   }

   [Fact]
   public async Task CanJoinLobby_ReturnsNameTaken_WhenUserAlreadyInLobby()
   {
      var svc = CreateService();
      var code = await svc.CreateLobbyWithSettings(2, 1, true, null);
      var user = new Guest { Id = Guid.NewGuid(), Name = "p" };
      await svc.JoinLobby(code, user);

      var msg = svc.CanJoinLobby(code, user.Id);
      Assert.Equal("Name already taken.", msg);
   }

   [Fact]
   public async Task CreateLobbyWithSettings_RandomGames_CreatesSessionWithExpectedRounds()
   {
      var svc = CreateService();
      var code = await svc.CreateLobbyWithSettings(2, 3, true, null);

      var session = svc.GetTournamentSession(code);
      Assert.NotNull(session);
      Assert.Equal(3, session!.GameTypesByRounds.Count);
      Assert.All(session.GameTypesByRounds, g => Assert.True(Enum.IsDefined(typeof(GameType), g)));
   }

   [Fact]
   public async Task CreateLobbyWithSettings_UsesProvidedGames_WhenRandomFalse()
   {
      var svc = CreateService();
      var games = new List<string> { "TicTacToe", "RockPaperScissors" };
      var code = await svc.CreateLobbyWithSettings(2, 2, false, games);

      var session = svc.GetTournamentSession(code);
      Assert.NotNull(session);
      Assert.Equal(new List<GameType> { GameType.TicTacToe, GameType.RockPaperScissors }, session!.GameTypesByRounds);
   }

   [Fact]
   public async Task GetPlayersInLobbyDTOs_ReturnsNamesAndZeroWins()
   {
      var svc = CreateService();
      var code = await svc.CreateLobbyWithSettings(2, 1, true, null);
      await svc.JoinLobby(code, new Guest { Id = Guid.NewGuid(), Name = "a" });
      await svc.JoinLobby(code, new Guest { Id = Guid.NewGuid(), Name = "b" });

      var dtos = await svc.GetPlayersInLobbyDTOs(code);
      Assert.Equal(2, dtos.Count);
      Assert.All(dtos, p => Assert.Equal(0, p.Wins));
      Assert.Contains(dtos, p => p.Name == "a");
      Assert.Contains(dtos, p => p.Name == "b");
   }
}

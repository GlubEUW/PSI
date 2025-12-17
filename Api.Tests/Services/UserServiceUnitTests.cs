using Api.Data;
using Api.Entities;
using Api.Models;
using Api.Services;

using Microsoft.EntityFrameworkCore;

namespace Api.Tests.Services;

public class UserServiceUnitTests
{
   [Fact]
   public async Task GetUserByIdAsync_ReturnsUser_WhenExists()
   {
      DatabaseContext context = TestHelpers.BuildInMemoryDbContext();
      Guid userId = Guid.NewGuid();
      Guest guest = new Guest { Id = userId, Name = "G" };
      context.Users.Add(guest);
      await context.SaveChangesAsync();

      IUserService service = new UserService(context);

      User? user = await service.GetUserByIdAsync(userId);

      Guest result = Assert.IsType<Guest>(user);
      Assert.Equal(userId, result.Id);
      Assert.Equal("G", result.Name);
   }

   [Fact]
   public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
   {
      DatabaseContext context = TestHelpers.BuildInMemoryDbContext();
      IUserService service = new UserService(context);

      User? user = await service.GetUserByIdAsync(Guid.NewGuid());

      Assert.Null(user);
   }

   [Fact]
   public async Task GetUserStatsAsync_ReturnsZero_WhenUserHasNoGames()
   {
      DatabaseContext context = TestHelpers.BuildInMemoryDbContext();
      Guid userId = Guid.NewGuid();
      context.Users.Add(new Guest { Id = userId, Name = "U" });
      await context.SaveChangesAsync();

      IUserService service = new UserService(context);

      GameStatsDto stats = await service.GetUserStatsAsync(userId);

      Assert.Equal(userId, stats.UserId);
      Assert.Equal(0, stats.TotalGamesPlayed);
      Assert.Equal(0, stats.TotalWins);
      Assert.Equal(0, stats.TicTacToeGamesPlayed);
      Assert.Equal(0, stats.TicTacToeWins);
      Assert.Equal(0, stats.RockPaperScissorsGamesPlayed);
      Assert.Equal(0, stats.RockPaperScissorsWins);
      Assert.Equal(0, stats.ConnectFourGamesPlayed);
      Assert.Equal(0, stats.ConnectFourWins);
   }

   [Fact]
   public async Task GetUserStatsAsync_ComputesTotalsAndWins_PerGameType()
   {
      DatabaseContext context = TestHelpers.BuildInMemoryDbContext();
      Guid userId = Guid.NewGuid();
      context.Users.Add(new Guest { Id = userId, Name = "U" });
      await context.SaveChangesAsync();

      Guid tournamentId = Guid.NewGuid();

      Game ttt1 = new Game { Id = Guid.NewGuid(), TournamentId = tournamentId, GameType = "TicTacToe", RoundNumber = 1 };
      Game ttt2 = new Game { Id = Guid.NewGuid(), TournamentId = tournamentId, GameType = "TicTacToe", RoundNumber = 1 };
      Game rps1 = new Game { Id = Guid.NewGuid(), TournamentId = tournamentId, GameType = "RockPaperScissors", RoundNumber = 1 };
      Game rps2 = new Game { Id = Guid.NewGuid(), TournamentId = tournamentId, GameType = "RockPaperScissors", RoundNumber = 1 };
      Game c4 = new Game { Id = Guid.NewGuid(), TournamentId = tournamentId, GameType = "ConnectFour", RoundNumber = 1 };
      context.Games.AddRange(ttt1, ttt2, rps1, rps2, c4);

      UserGame ur1 = new UserGame { UserId = userId, GameId = ttt1.Id, PlayerTurn = 1, PlayerPlacement = 1 };
      UserGame ur2 = new UserGame { UserId = userId, GameId = ttt2.Id, PlayerTurn = 2, PlayerPlacement = 2 };
      UserGame ur3 = new UserGame { UserId = userId, GameId = rps1.Id, PlayerTurn = 1, PlayerPlacement = 1 };
      UserGame ur4 = new UserGame { UserId = userId, GameId = rps2.Id, PlayerTurn = 2, PlayerPlacement = 1 };
      UserGame ur5 = new UserGame { UserId = userId, GameId = c4.Id, PlayerTurn = 1, PlayerPlacement = 2 };
      context.UserRound.AddRange(ur1, ur2, ur3, ur4, ur5);

      await context.SaveChangesAsync();

      IUserService service = new UserService(context);

      GameStatsDto stats = await service.GetUserStatsAsync(userId);

      Assert.Equal(userId, stats.UserId);
      Assert.Equal(5, stats.TotalGamesPlayed);
      Assert.Equal(3, stats.TotalWins);

      Assert.Equal(2, stats.TicTacToeGamesPlayed);
      Assert.Equal(1, stats.TicTacToeWins);

      Assert.Equal(2, stats.RockPaperScissorsGamesPlayed);
      Assert.Equal(2, stats.RockPaperScissorsWins);

      Assert.Equal(1, stats.ConnectFourGamesPlayed);
      Assert.Equal(0, stats.ConnectFourWins);
   }

   [Fact]
   public async Task GetUserStatsAsync_IncludesUnknownGameTypes_InTotalsOnly()
   {
      DatabaseContext context = TestHelpers.BuildInMemoryDbContext();
      Guid userId = Guid.NewGuid();
      context.Users.Add(new Guest { Id = userId, Name = "U" });
      await context.SaveChangesAsync();

      Guid tournamentId = Guid.NewGuid();
      Game unknown = new Game { Id = Guid.NewGuid(), TournamentId = tournamentId, GameType = "Unknown", RoundNumber = 1 };
      context.Games.Add(unknown);
      context.UserRound.Add(new UserGame { UserId = userId, GameId = unknown.Id, PlayerTurn = 1, PlayerPlacement = 1 });
      await context.SaveChangesAsync();

      IUserService service = new UserService(context);

      GameStatsDto stats = await service.GetUserStatsAsync(userId);

      Assert.Equal(1, stats.TotalGamesPlayed);
      Assert.Equal(1, stats.TotalWins);
      Assert.Equal(0, stats.TicTacToeGamesPlayed);
      Assert.Equal(0, stats.RockPaperScissorsGamesPlayed);
      Assert.Equal(0, stats.ConnectFourGamesPlayed);
   }
}

using Api.Controllers;
using Api.Data;
using Api.Entities;
using Api.Models;
using Api.Services;

using Microsoft.AspNetCore.Mvc;

using Moq;

namespace Api.Tests.Controllers;

public class LeaderBoardControllerUnitTests
{
   [Fact]
   public async Task Get_ReturnsOkWithDto_FromService()
   {
      var dto = new LeaderBoardPageDto
      {
         Page = 1,
         PageSize = 50,
         TotalUsers = 1,
         Users = new List<LeaderBoardUserDto>
         {
            new LeaderBoardUserDto { Id = Guid.NewGuid(), Name = "A", TotalWins = 7 }
         }
      };

      var service = new Mock<ILeaderBoardService>();
      service.Setup(s => s.GetLeaderBoardAsync(1, 50)).ReturnsAsync(dto);

      var controller = new LeaderBoardController(service.Object);

      ActionResult<LeaderBoardPageDto> result = await controller.Get(1, 50);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var okDto = Assert.IsType<LeaderBoardPageDto>(ok.Value);
      Assert.Same(dto, okDto);

      service.Verify(s => s.GetLeaderBoardAsync(1, 50), Times.Once);
   }

   [Fact]
   public async Task Get_ExcludesGuests()
   {
      DatabaseContext context = BuildSeededContext();
      var service = new LeaderBoardService(context);
      var controller = new LeaderBoardController(service);

      ActionResult<LeaderBoardPageDto> result = await controller.Get(0, 100);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var page = Assert.IsType<LeaderBoardPageDto>(ok.Value);

      Assert.Equal(2, page.TotalUsers);
      Assert.Equal(2, page.Users.Count);
      Assert.DoesNotContain(page.Users, u => u.Name == "Guest");
   }

   [Fact]
   public async Task Get_SortsByTotalWinsDescending_ThenName()
   {
      DatabaseContext context = BuildSeededContext();
      var service = new LeaderBoardService(context);
      var controller = new LeaderBoardController(service);

      ActionResult<LeaderBoardPageDto> result = await controller.Get(0, 100);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var page = Assert.IsType<LeaderBoardPageDto>(ok.Value);

      Assert.Equal("user1", page.Users[0].Name);
      Assert.Equal(3, page.Users[0].TotalWins);
      Assert.Equal("user2", page.Users[1].Name);
      Assert.Equal(2, page.Users[1].TotalWins);
   }

   [Fact]
   public async Task Get_Paginates()
   {
      DatabaseContext context = BuildSeededContext();
      var service = new LeaderBoardService(context);
      var controller = new LeaderBoardController(service);

      ActionResult<LeaderBoardPageDto> page0Result = await controller.Get(0, 1);
      var page0 = Assert.IsType<LeaderBoardPageDto>(Assert.IsType<OkObjectResult>(page0Result.Result).Value);
      Assert.Equal(2, page0.TotalUsers);
      Assert.Equal(0, page0.Page);
      Assert.Equal(1, page0.PageSize);
      Assert.Single(page0.Users);
      Assert.Equal("user1", page0.Users[0].Name);

      ActionResult<LeaderBoardPageDto> page1Result = await controller.Get(1, 1);
      var page1 = Assert.IsType<LeaderBoardPageDto>(Assert.IsType<OkObjectResult>(page1Result.Result).Value);
      Assert.Equal(2, page1.TotalUsers);
      Assert.Equal(1, page1.Page);
      Assert.Equal(1, page1.PageSize);
      Assert.Single(page1.Users);
      Assert.Equal("user2", page1.Users[0].Name);
   }

   [Fact]
   public async Task Get_ClampsPageSizeTo100()
   {
      DatabaseContext context = BuildSeededContext();
      var service = new LeaderBoardService(context);
      var controller = new LeaderBoardController(service);

      ActionResult<LeaderBoardPageDto> result = await controller.Get(0, 1000);

      var ok = Assert.IsType<OkObjectResult>(result.Result);
      var page = Assert.IsType<LeaderBoardPageDto>(ok.Value);

      Assert.Equal(100, page.PageSize);
      Assert.Equal(2, page.Users.Count);
   }

   private static DatabaseContext BuildSeededContext()
   {
      DatabaseContext context = TestHelpers.BuildInMemoryDbContext($"LeaderBoard_{Guid.NewGuid()}");

      var user2 = new RegisteredUser { Id = Guid.NewGuid(), Name = "user2", PasswordHash = "hash" };
      var user1 = new RegisteredUser { Id = Guid.NewGuid(), Name = "user1", PasswordHash = "hash" };
      var guest = new Guest { Id = Guid.NewGuid(), Name = "Guest" };

      var g1 = new Game { Id = Guid.NewGuid(), TournamentId = Guid.NewGuid(), GameType = "TicTacToe", RoundNumber = 0 };
      var g2 = new Game { Id = Guid.NewGuid(), TournamentId = Guid.NewGuid(), GameType = "RockPaperScissors", RoundNumber = 0 };
      var g3 = new Game { Id = Guid.NewGuid(), TournamentId = Guid.NewGuid(), GameType = "ConnectFour", RoundNumber = 0 };
      var g4 = new Game { Id = Guid.NewGuid(), TournamentId = Guid.NewGuid(), GameType = "ConnectFour", RoundNumber = 1 };
      var g5 = new Game { Id = Guid.NewGuid(), TournamentId = Guid.NewGuid(), GameType = "TicTacToe", RoundNumber = 1 };

      context.Users.AddRange(user2, user1, guest);
      context.Games.AddRange(g1, g2, g3, g4, g5);

      var rounds = new List<UserGame>
      {
         new UserGame { UserId = user2.Id, GameId = g1.Id, PlayerTurn = 0, PlayerPlacement = 1 },
         new UserGame { UserId = user2.Id, GameId = g2.Id, PlayerTurn = 0, PlayerPlacement = 1 },
         new UserGame { UserId = user1.Id, GameId = g3.Id, PlayerTurn = 0, PlayerPlacement = 1 },
         new UserGame { UserId = user1.Id, GameId = g4.Id, PlayerTurn = 0, PlayerPlacement = 1 },
         new UserGame { UserId = user1.Id, GameId = g5.Id, PlayerTurn = 0, PlayerPlacement = 1 },
         new UserGame { UserId = guest.Id, GameId = g2.Id, PlayerTurn = 1, PlayerPlacement = 1 }
      };

      context.UserRound.AddRange(rounds);
      context.SaveChanges();

      return context;
   }
}

using Api.Entities;
using Api.Enums;
using Api.Services;

namespace Api.Tests.Services;

public class GameServiceUnitTests
{
   [Fact]
   public void IsValidGameType_ReturnsTrue_ForKnownGameTypeString()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      Assert.True(svc.IsValidGameType("TicTacToe"));
   }

   [Fact]
   public void IsValidGameType_ReturnsFalse_ForUnknownGameTypeString()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      Assert.False(svc.IsValidGameType("NotARealGame"));
   }

   [Fact]
   public void IsValidGameType_ReturnsTrue_ForKnownEnum()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      Assert.True(svc.IsValidGameType(GameType.TicTacToe));
   }

   [Fact]
   public void StartGame_ReturnsNull_WhenPlayersNullOrTooFew()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      Assert.Null(svc.StartGame(GameType.TicTacToe, null!));

      var onePlayer = new List<User> { new Guest { Id = Guid.NewGuid(), Name = "p1" } };
      Assert.Null(svc.StartGame(GameType.TicTacToe, onePlayer));
   }

   [Fact]
   public void StartGame_ReturnsGame_WhenValid()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      var players = new List<User>
      {
         new Guest { Id = Guid.NewGuid(), Name = "a" },
         new Guest { Id = Guid.NewGuid(), Name = "b" }
      };

      var game = svc.StartGame(GameType.TicTacToe, players);
      Assert.NotNull(game);
      Assert.Equal(GameType.TicTacToe, game!.GameType);
   }
}

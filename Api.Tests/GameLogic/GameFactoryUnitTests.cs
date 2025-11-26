using Api.Entities;
using Api.Enums;
using Api.GameLogic;

namespace Api.Tests.GameLogic;

public class GameFactoryUnitTests
{
   [Fact]
   public void ValidGameTypes_Contains_All()
   {
      var factory = new GameFactory();
      Assert.Contains(GameType.TicTacToe, factory.ValidGameTypes);
      Assert.Contains(GameType.RockPaperScissors, factory.ValidGameTypes);
      Assert.Contains(GameType.ConnectFour, factory.ValidGameTypes);
   }

   [Theory]
   [InlineData(GameType.TicTacToe, typeof(TicTacToeGame))]
   [InlineData(GameType.RockPaperScissors, typeof(RockPaperScissorsGame))]
   [InlineData(GameType.ConnectFour, typeof(ConnectFourGame))]
   public void CreateGame_Returns_Correct_Type(GameType type, Type expected)
   {
      var players = new List<User> { new Guest { Name = "A" }, new Guest { Name = "B" } };
      var factory = new GameFactory();
      var game = factory.CreateGame(type, players);
      Assert.IsType(expected, game);
      Assert.Equal(type, game.GameType);
   }

   [Fact]
   public void CreateGame_UnknownType_Throws()
   {
      var players = new List<User> { new Guest { Name = "A" }, new Guest { Name = "B" } };
      var factory = new GameFactory();
      Assert.Throws<ArgumentException>(() => factory.CreateGame((GameType)999, players));
   }
}

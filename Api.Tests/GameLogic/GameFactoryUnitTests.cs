using Api.Entities;
using Api.GameLogic;

namespace Api.Tests.GameLogic;

public class GameFactoryUnitTests
{
   [Fact]
   public void ValidGameTypes_Contains_All()
   {
      var factory = new GameFactory();
      Assert.Contains("TicTacToe", factory.ValidGameTypes);
      Assert.Contains("RockPaperScissors", factory.ValidGameTypes);
      Assert.Contains("ConnectFour", factory.ValidGameTypes);
   }

   [Theory]
   [InlineData("TicTacToe", typeof(TicTacToeGame))]
   [InlineData("RockPaperScissors", typeof(RockPaperScissorsGame))]
   [InlineData("ConnectFour", typeof(ConnectFourGame))]
   public void CreateGame_Returns_Correct_Type(string type, Type expected)
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
      Assert.Throws<ArgumentException>(() => factory.CreateGame("UnknownType", players));
   }
}

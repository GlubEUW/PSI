using System.Text.Json;

using Api.Entities;
using Api.GameLogic;
using Api.Exceptions;

namespace Api.Tests.GameLogic;

public class ConnectFourGameUnitTests
{
   private static JsonElement Move(User player, int column)
   {
      var payload = new { Column = column };
      return JsonSerializer.SerializeToElement(payload);
   }

   private static (ConnectFourGame game, Guest p1, Guest p2) CreateGame()
   {
      var p1 = TestHelpers.BuildGuest("Player1");
      var p2 = TestHelpers.BuildGuest("Player2");
      var game = new ConnectFourGame(new List<User> { p1, p2 });
      return (game, p1, p2);
   }

   [Fact]
   public void Rejects_Invalid_Column_And_Wrong_Turn()
   {
      var (game, p1, p2) = CreateGame();
      Assert.False(game.MakeMove(Move(p2, 0), p2));
      Assert.Throws<InvalidMoveException>(() => game.MakeMove(Move(p1, -1), p1));
      Assert.Throws<InvalidMoveException>(() => game.MakeMove(Move(p1, 7), p1));
      Assert.True(game.MakeMove(Move(p1, 0), p1));
   }

   [Fact]
   public void Rejects_Full_Column()
   {
      var (game, p1, p2) = CreateGame();
      for (var i = 0; i < 3; i++)
      {
         Assert.True(game.MakeMove(Move(p1, 0), p1));
         Assert.True(game.MakeMove(Move(p2, 0), p2));
      }
      Assert.False(game.MakeMove(Move(p1, 0), p1));
   }

   [Fact]
   public void Vertical_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();
      Assert.True(game.MakeMove(Move(p1, 0), p1));
      Assert.True(game.MakeMove(Move(p2, 1), p2));
      Assert.True(game.MakeMove(Move(p1, 0), p1));
      Assert.True(game.MakeMove(Move(p2, 1), p2));
      Assert.True(game.MakeMove(Move(p1, 0), p1));
      Assert.True(game.MakeMove(Move(p2, 1), p2));
      Assert.True(game.MakeMove(Move(p1, 0), p1));

      var state = game.GetState();
      var winner = (User?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal(p1, winner);
      Assert.False(game.MakeMove(Move(p2, 2), p2));
   }

   [Fact]
   public void Horizontal_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();

      Assert.True(game.MakeMove(Move(p1, 0), p1));
      Assert.True(game.MakeMove(Move(p2, 6), p2));
      Assert.True(game.MakeMove(Move(p1, 1), p1));
      Assert.True(game.MakeMove(Move(p2, 6), p2));
      Assert.True(game.MakeMove(Move(p1, 2), p1));
      Assert.True(game.MakeMove(Move(p2, 6), p2));
      Assert.True(game.MakeMove(Move(p1, 3), p1));

      var state = game.GetState();
      var winner = (User?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal(p1, winner);
   }

   [Fact]
   public void Diagonal_PositiveSlope_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();

      Assert.True(game.MakeMove(Move(p1, 0), p1));
      Assert.True(game.MakeMove(Move(p2, 1), p2));

      Assert.True(game.MakeMove(Move(p1, 1), p1));
      Assert.True(game.MakeMove(Move(p2, 2), p2));

      Assert.True(game.MakeMove(Move(p1, 2), p1));
      Assert.True(game.MakeMove(Move(p2, 3), p2));

      Assert.True(game.MakeMove(Move(p1, 2), p1));
      Assert.True(game.MakeMove(Move(p2, 3), p2));

      Assert.True(game.MakeMove(Move(p1, 3), p1));
      Assert.True(game.MakeMove(Move(p2, 6), p2));

      Assert.True(game.MakeMove(Move(p1, 3), p1));

      var state = game.GetState();
      var winner = (User?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal(p1, winner);
      Assert.False(game.MakeMove(Move(p2, 0), p2));
   }

   [Fact]
   public void Diagonal_NegativeSlope_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();

      Assert.True(game.MakeMove(Move(p1, 3), p1));
      Assert.True(game.MakeMove(Move(p2, 4), p2));

      Assert.True(game.MakeMove(Move(p1, 4), p1));
      Assert.True(game.MakeMove(Move(p2, 5), p2));

      Assert.True(game.MakeMove(Move(p1, 5), p1));
      Assert.True(game.MakeMove(Move(p2, 6), p2));

      Assert.True(game.MakeMove(Move(p1, 5), p1));
      Assert.True(game.MakeMove(Move(p2, 6), p2));

      Assert.True(game.MakeMove(Move(p1, 6), p1));
      Assert.True(game.MakeMove(Move(p2, 2), p2));

      Assert.True(game.MakeMove(Move(p1, 6), p1));

      var state = game.GetState();
      var winner = (User?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal(p1, winner);
      Assert.False(game.MakeMove(Move(p2, 0), p2));
   }

   [Fact(Skip = "To be implemented")]
   public void Draw_When_Board_Filled_No_Four_Aligned()
   {
   }
}

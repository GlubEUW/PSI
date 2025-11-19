using System.Text.Json;

using Api.Entities;
using Api.GameLogic;

namespace Api.Tests.GameLogic;

public class ConnectFourGameUnitTests
{
   private static JsonElement Move(Guid playerId, int column)
   {
      var payload = new ConnectFourMove { PlayerId = playerId, Column = column };
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
      Assert.False(game.MakeMove(Move(p2.Id, 0)));
      Assert.False(game.MakeMove(Move(p1.Id, -1)));
      Assert.False(game.MakeMove(Move(p1.Id, 7)));
      Assert.True(game.MakeMove(Move(p1.Id, 0)));
   }

   [Fact]
   public void Rejects_Full_Column()
   {
      var (game, p1, p2) = CreateGame();
      for (var i = 0; i < 3; i++)
      {
         Assert.True(game.MakeMove(Move(p1.Id, 0)));
         Assert.True(game.MakeMove(Move(p2.Id, 0)));
      }
      Assert.False(game.MakeMove(Move(p1.Id, 0)));
   }

   [Fact]
   public void Vertical_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();
      Assert.True(game.MakeMove(Move(p1.Id, 0)));
      Assert.True(game.MakeMove(Move(p2.Id, 1)));
      Assert.True(game.MakeMove(Move(p1.Id, 0)));
      Assert.True(game.MakeMove(Move(p2.Id, 1)));
      Assert.True(game.MakeMove(Move(p1.Id, 0)));
      Assert.True(game.MakeMove(Move(p2.Id, 1)));
      Assert.True(game.MakeMove(Move(p1.Id, 0)));

      var state = game.GetState();
      var winner = (string?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal("Player1", winner);
      Assert.Equal(1, game.Players[0].Wins);
      Assert.False(game.MakeMove(Move(p2.Id, 2)));
   }

   [Fact]
   public void Horizontal_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();
      Assert.True(game.MakeMove(Move(p1.Id, 0)));
      Assert.True(game.MakeMove(Move(p2.Id, 6)));
      Assert.True(game.MakeMove(Move(p1.Id, 1)));
      Assert.True(game.MakeMove(Move(p2.Id, 6)));
      Assert.True(game.MakeMove(Move(p1.Id, 2)));
      Assert.True(game.MakeMove(Move(p2.Id, 6)));
      Assert.True(game.MakeMove(Move(p1.Id, 3)));

      var state = game.GetState();
      var winner = (string?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal("Player1", winner);
      Assert.Equal(1, game.Players[0].Wins);
   }

   [Fact]
   public void Diagonal_PositiveSlope_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();

      var R = (int)DiscColor.Red;
      var Y = (int)DiscColor.Yellow;




      game.Board[5][0] = R;
      game.Board[5][1] = Y; game.Board[4][1] = R;
      game.Board[5][2] = Y; game.Board[4][2] = Y; game.Board[3][2] = R;
      game.Board[5][3] = Y; game.Board[4][3] = Y; game.Board[3][3] = Y; // below (2,3)


      game.PlayerTurn = p1.Id;
      Assert.True(game.MakeMove(Move(p1.Id, 3)));

      var state = game.GetState();
      var winner = (string?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal("Player1", winner);
      Assert.Equal(1, game.Players[0].Wins);
   }

   [Fact]
   public void Diagonal_NegativeSlope_Win_For_First_Player()
   {
      var (game, p1, p2) = CreateGame();
      var R = (int)DiscColor.Red;
      var Y = (int)DiscColor.Yellow;




      game.Board[5][6] = R;
      game.Board[5][5] = Y; game.Board[4][5] = R;
      game.Board[5][4] = Y; game.Board[4][4] = Y; game.Board[3][4] = R;
      game.Board[5][3] = Y; game.Board[4][3] = Y; game.Board[3][3] = Y; // below (2,3)

      game.PlayerTurn = p1.Id;
      Assert.True(game.MakeMove(Move(p1.Id, 3)));

      var state = game.GetState();
      var winner = (string?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal("Player1", winner);
      Assert.Equal(1, game.Players[0].Wins);
   }

   [Fact]
   public void Draw_When_Board_Filled_No_Four_Aligned()
   {
      var (game, p1, p2) = CreateGame();
      var R = (int)DiscColor.Red;
      var Y = (int)DiscColor.Yellow;


      for (var r = 0; r < 6; r++)
      {
         for (var c = 0; c < 7; c++)
         {
            if (r == 0 && c == 0) continue; // leave top-left empty for the last move
            game.Board[r][c] = ((r + c) % 2 == 0) ? Y : R;
         }
      }


      game.PlayerTurn = p1.Id;
      Assert.True(game.MakeMove(Move(p1.Id, 0)));

      var state = game.GetState();
      var winner = (string?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal("Draw", winner);
   }
}

using System.Text.Json;

using Api.Entities;
using Api.GameLogic;
using Api.Exceptions;

namespace Api.Tests.GameLogic;

public class TicTacToeGameUnitTests
{
   private static JsonElement Move(User player, int x, int y)
   {
      var payload = new { Player = player, X = x, Y = y };
      return JsonSerializer.SerializeToElement(payload);
   }

   private static (TicTacToeGame game, Guest p1, Guest p2) CreateGame()
   {
      var p1 = TestHelpers.BuildGuest("Player1");
      var p2 = TestHelpers.BuildGuest("Player2");
      var game = new TicTacToeGame(new List<User> { p1, p2 });
      return (game, p1, p2);
   }

   [Fact]
   public void Initial_State_Has_EmptyBoard_And_FirstPlayerTurn()
   {
      var (game, p1, _) = CreateGame();

      var state = game.GetState();
      var board = (int[][])state.GetType().GetProperty("Board")!.GetValue(state)!;
      var playerTurn = (User)state.GetType().GetProperty("PlayerTurn")!.GetValue(state)!;
      var winner = (User?)state.GetType().GetProperty("Winner")!.GetValue(state);

      Assert.All(board.SelectMany(r => r), cell => Assert.Equal(2, cell));
      Assert.Equal(p1, playerTurn);
      Assert.Null(winner);
   }

   [Fact]
   public void MakeMove_Rejects_InvalidJson()
   {
      var (game, _, _) = CreateGame();
      var invalid = JsonDocument.Parse("{\"foo\":1}").RootElement;
      Assert.False(game.MakeMove(invalid));
   }

   [Fact]
   public void MakeMove_Enforces_Turn_And_Cell_Occupancy()
   {
      var (game, p1, p2) = CreateGame();

      Assert.False(game.MakeMove(Move(p2, 0, 0)));

      Assert.True(game.MakeMove(Move(p1, 0, 0)));

      Assert.Throws<InvalidMoveException>(() => game.MakeMove(Move(p2, 0, 0)));

      Assert.True(game.MakeMove(Move(p2, 1, 1)));
   }

   [Fact]
   public void X_Wins_Row_And_Wins_Incremented()
   {
      var (game, p1, p2) = CreateGame();

      Assert.True(game.MakeMove(Move(p1, 0, 0)));
      Assert.True(game.MakeMove(Move(p2, 1, 0)));
      Assert.True(game.MakeMove(Move(p1, 0, 1)));
      Assert.True(game.MakeMove(Move(p2, 1, 1)));
      Assert.True(game.MakeMove(Move(p1, 0, 2)));

      var state = game.GetState();
      var winner = (User?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Equal(p1, winner);

      Assert.False(game.MakeMove(Move(p2, 2, 2)));
   }

   [Fact]
   public void Draw_After_Filling_Board_No_Winner()
   {
      var (game, p1, p2) = CreateGame();

      var seq = new (User player, int x, int y)[]
      {
            (p1,0,0),(p2,0,1),(p1,0,2),
            (p2,1,1),(p1,1,0),(p2,1,2),
            (p1,2,1),(p2,2,0),(p1,2,2)
      };

      foreach (var (pl, x, y) in seq)
         Assert.True(game.MakeMove(Move(pl, x, y)));

      var state = game.GetState();
      var winner = (User?)state.GetType().GetProperty("Winner")!.GetValue(state);
      Assert.Null(winner);
   }
}

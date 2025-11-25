using System.Text.Json;

using Api.Entities;
using Api.GameLogic;

namespace Api.Tests.GameLogic;

public class RockPaperScissorsGameUnitTests
{
   public enum RockPaperScissorsChoice
   {
      Rock = 0,
      Paper = 1,
      Scissors = 2
   }

   private static JsonElement Move(User player, RockPaperScissorsChoice choice)
   {
      var payload = new { Player = player, Choice = choice };
      return JsonSerializer.SerializeToElement(payload);
   }

   private static (RockPaperScissorsGame game, Guest p1, Guest p2) CreateGame()
   {
      var p1 = TestHelpers.BuildGuest("Player1");
      var p2 = TestHelpers.BuildGuest("Player2");
      var game = new RockPaperScissorsGame(new List<User> { p1, p2 });
      return (game, p1, p2);
   }

   [Fact]
   public void Result_Remains_Null_Until_Both_Move()
   {
      var (game, p1, _) = CreateGame();
      Assert.True(game.MakeMove(Move(p1, RockPaperScissorsChoice.Rock)));

      var state = game.GetState();
      var result = (string?)state.GetType().GetProperty("Result")!.GetValue(state);
      Assert.Null(result);
   }

   [Fact]
   public void Determines_Draw()
   {
      var (game, p1, p2) = CreateGame();
      Assert.True(game.MakeMove(Move(p1, RockPaperScissorsChoice.Rock)));
      Assert.True(game.MakeMove(Move(p2, RockPaperScissorsChoice.Rock)));

      var state = game.GetState();
      var result = (string?)state.GetType().GetProperty("Result")!.GetValue(state);
      Assert.Equal("Draw!", result);
   }

   [Fact]
   public void Determines_Winner_And_Increments_Wins()
   {
      var (game, p1, p2) = CreateGame();
      Assert.True(game.MakeMove(Move(p1, RockPaperScissorsChoice.Rock)));
      Assert.True(game.MakeMove(Move(p2, RockPaperScissorsChoice.Scissors)));

      var state = game.GetState();
      var result = (string?)state.GetType().GetProperty("Result")!.GetValue(state);
      Assert.Equal($"{p1} wins!", result);
   }

   [Theory]
   [InlineData(RockPaperScissorsChoice.Rock, RockPaperScissorsChoice.Scissors)]
   [InlineData(RockPaperScissorsChoice.Paper, RockPaperScissorsChoice.Rock)]
   [InlineData(RockPaperScissorsChoice.Scissors, RockPaperScissorsChoice.Paper)]
   public void Player1_Wins_For_All_Winning_Combinations(RockPaperScissorsChoice c1, RockPaperScissorsChoice c2)
   {
      var (game, p1, p2) = CreateGame();
      Assert.True(game.MakeMove(Move(p1, c1)));
      Assert.True(game.MakeMove(Move(p2, c2)));

      var state = game.GetState();
      var result = (string?)state.GetType().GetProperty("Result")!.GetValue(state);
      Assert.Equal($"{p1} wins!", result);
   }

   [Fact]
   public void Invalid_Json_Returns_False()
   {
      var (game, _, _) = CreateGame();
      var invalid = JsonDocument.Parse("{\"bogus\":true}").RootElement;
      Assert.False(game.MakeMove(invalid));
   }
}

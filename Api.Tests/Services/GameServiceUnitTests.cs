using System.Text.Json;

using Api.Services;
using Api.Entities;


namespace Api.Tests.Services;

public class GameServiceUnitTests
{
   [Fact]
   public void StartGame_ReturnsFalse_WhenPlayersNullOrTooFew()
   {
      var svc = new GameService();

      Assert.False(svc.StartGame("g1", "TicTacToe", null!));

      var onePlayer = new List<User> { new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "p1" } };
      Assert.False(svc.StartGame("g2", "TicTacToe", onePlayer));
   }

   [Fact]
   public void StartGame_ReturnsFalse_WhenGameIdExists()
   {
      var svc = new GameService();
      var players = new List<User>
      {
         new Guest { Id = Guid.NewGuid(), Name = "a" },
         new Guest { Id = Guid.NewGuid(), Name = "b" }
      };

      var id = "dup";
      Assert.True(svc.StartGame(id, "TicTacToe", players));
      // second start with same id should fail
      Assert.False(svc.StartGame(id, "TicTacToe", players));

      // cleanup
      svc.RemoveGame(id);
   }

   [Fact]
   public void StartGame_ReturnsTrue_And_GetGameState_NotNull()
   {
      var svc = new GameService();
      var players = new List<User>
      {
         new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "alice" },
         new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "bob" }
      };

      var id = Guid.NewGuid().ToString();
      var ok = svc.StartGame(id, "TicTacToe", players);
      Assert.True(ok);

      var state = svc.GetGameState(id);
      Assert.NotNull(state);

      // state should expose Board and PlayerTurn via anonymous object
      var boardProp = state!.GetType().GetProperty("Board");
      Assert.NotNull(boardProp);

      svc.RemoveGame(id);
   }

   [Fact]
   public void RemoveGame_ReturnsFalse_WhenNotExists()
   {
      var svc = new GameService();
      Assert.False(svc.RemoveGame("no-such-game"));
   }

   [Fact]
   public void MakeMove_ReturnsFalse_WhenGameNotFound()
   {
      var svc = new GameService();
      var fakeMove = JsonDocument.Parse("{}").RootElement;
      Assert.False(svc.MakeMove("missing", fakeMove, out var _));
   }

   [Fact]
   public void MakeMove_TicTacToe_ValidMove_UpdatesState()
   {
      var svc = new GameService();
      var p1 = new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "p1" };
      var p2 = new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "p2" };
      var players = new List<User> { p1, p2 };
      var id = Guid.NewGuid().ToString();

      Assert.True(svc.StartGame(id, "TicTacToe", players));

      // create a move for player1 at (0,0)
      var moveJson = JsonSerializer.Serialize(new
      {
         PlayerId = p1.Id,
         X = 0,
         Y = 0
      });

      var elem = JsonDocument.Parse(moveJson).RootElement;

      Assert.True(svc.MakeMove(id, elem, out var newState));
      Assert.NotNull(newState);

      var boardProp = newState!.GetType().GetProperty("Board");
      Assert.NotNull(boardProp);

      // cleanup
      svc.RemoveGame(id);
   }

   [Fact]
   public void GetGameState_ReturnsNull_WhenNotFound()
   {
      var svc = new GameService();
      Assert.Null(svc.GetGameState("nope"));
   }
}

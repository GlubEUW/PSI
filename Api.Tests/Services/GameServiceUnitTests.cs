using System.Text.Json;
using Api.Services;
using Api.Entities;


namespace Api.Tests.Services;

public class GameServiceUnitTests
{
   [Fact]
   public void StartGame_ReturnsFalse_WhenPlayersNullOrTooFew()
   {
      var svc = (GameService)TestHelpers.CreateGameService();

      Assert.False(svc.StartGame("g1", "gameType1", null!));

      var onePlayer = new List<User> { new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "p1" } };
      Assert.False(svc.StartGame("g2", "gameType1", onePlayer));
   }

   [Fact]
   public void StartGame_ReturnsFalse_WhenGameIdExists()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      var players = new List<User>
      {
         new Guest { Id = Guid.NewGuid(), Name = "a" },
         new Guest { Id = Guid.NewGuid(), Name = "b" }
      };

      var id = "dup";
      Assert.True(svc.StartGame(id, "gameType1", players));
      Assert.False(svc.StartGame(id, "gameType1", players));

      svc.RemoveGame(id);
   }

   [Fact]
   public void StartGame_ReturnsTrue_And_GetGameState_NotNull()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      var players = new List<User>
      {
         new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "player1" },
         new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "player2" }
      };

      var id = Guid.NewGuid().ToString();
      var ok = svc.StartGame(id, "gameType1", players);
      Assert.True(ok);

      var state = svc.GetGameState(id);
      Assert.NotNull(state);

      var boardProp = state!.GetType().GetProperty("Board");
      Assert.NotNull(boardProp);

      svc.RemoveGame(id);
   }

   [Fact]
   public void StartGame_ReturnsFalse_WhenFactoryThrows_And_LogsError()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      var players = new List<User>
         {
            new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "player1" },
            new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "player2" }
         };

      var id = Guid.NewGuid().ToString();

      var sw = new StringWriter();
      var originalOut = Console.Out;
      try
      {
         Console.SetOut(sw);
         var ok = svc.StartGame(id, "ThisGameTypeDoesNotExist", players);
         Assert.False(ok);
      }
      finally
      {
         Console.SetOut(originalOut);
      }

      var output = sw.ToString();
      Assert.Contains("Error starting game", output);
      Assert.Null(svc.GetGameState(id));
   }

   [Fact]
   public void RemoveGame_ReturnsFalse_WhenNotExists()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      Assert.False(svc.RemoveGame("no-such-game"));
   }

   [Fact]
   public void MakeMove_ReturnsFalse_WhenGameNotFound()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      var fakeMove = JsonDocument.Parse("{}").RootElement;
      Assert.False(svc.MakeMove("missing", fakeMove, out var _));
   }

   [Fact]
   public void MakeMove_TicTacToe_ValidMove_UpdatesState()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      var p1 = new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "player1" };
      var p2 = new Api.Entities.Guest { Id = Guid.NewGuid(), Name = "player2" };
      var players = new List<User> { p1, p2 };
      var id = Guid.NewGuid().ToString();

      Assert.True(svc.StartGame(id, "gameType1", players));

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

      svc.RemoveGame(id);
   }

   [Fact]
   public void GetGameState_ReturnsNull_WhenNotFound()
   {
      var svc = (GameService)TestHelpers.CreateGameService();
      Assert.Null(svc.GetGameState("nope"));
   }
}

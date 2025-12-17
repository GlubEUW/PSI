using System.Text.Json;

using Api.Entities;
using Api.Enums;
using Api.GameLogic;

namespace Api.Tests.TestDoubles;

public sealed class TestGameFactory : IGameFactory
{
   private readonly HashSet<GameType> _validGameTypes = new()
   {
      GameType.TicTacToe,
      GameType.RockPaperScissors,
      GameType.ConnectFour
   };

   public IReadOnlySet<GameType> ValidGameTypes => _validGameTypes;

   public IGame CreateGame(GameType gameType, List<User> players)
   {
      if (!_validGameTypes.Contains(gameType))
         throw new ArgumentException($"Unknown game type: {gameType}");

      return new TestGame(gameType, players);
   }

   private sealed class TestGame : IGame
   {
      public User? Winner { get; private set; }
      public GameType GameType { get; }
      public bool GameOver { get; private set; }
      public List<User> Players { get; }
      private readonly TestState _state;

      public TestGame(GameType gameType, List<User> players)
      {
         GameType = gameType;
         Players = players;
         _state = new TestState
         {
            Board = new int[3, 3],
            Players = players.Select(p => new PlayerRef { Id = p.Id, Name = p.Name }).ToList(),
            Moves = new List<string>()
         };
      }

      public object GetState()
      {
         return _state;
      }

      public bool MakeMove(JsonElement moveData, User player)
      {
         if (!Players.Any(p => p.Id == player.Id))
            return false;

         _state.Moves.Add(moveData.ToString());
         _state.Board[0, 0] = _state.Board[0, 0] == 0 ? 1 : 0;

         if (_state.Moves.Count >= 3)
         {
            GameOver = true;
            Winner = Players.FirstOrDefault();
         }

         return true;
      }

      private sealed class TestState
      {
         public int[,] Board { get; set; } = new int[3, 3];
         public List<PlayerRef> Players { get; set; } = new();
         public List<string> Moves { get; set; } = new();
      }

      private sealed class PlayerRef
      {
         public Guid Id { get; set; }
         public string Name { get; set; } = string.Empty;
      }
   }
}

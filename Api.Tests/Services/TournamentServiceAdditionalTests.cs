using Api.Data;
using Api.Entities;
using Api.Enums;
using Api.GameLogic;
using Api.Models;
using Api.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

namespace Api.Tests.Services;

public class TournamentServiceAdditionalTests
{
    [Fact]
    public void GetGameListForCurrentRound_ReturnsGames_WhenExists()
    {
        var store = new TournamentStore();
        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");

        var game = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: false, winner: null);
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { p1, p2 }
        };

        session.GamesByPlayers[p1] = game;
        session.GamesByPlayers[p2] = game;
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        var games = service.getGameListForCurrentRound("CODE");

        Assert.NotEmpty(games);
        Assert.Equal(2, games.Count);
        Assert.True(games.ContainsKey(p1));
        Assert.True(games.ContainsKey(p2));
        Assert.Same(game, games[p1]);
    }

    [Fact]
    public void HalfPlayersReadyForNextRound_AlwaysReturnsTrue()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        Assert.True(service.HalfPlayersReadyForNextRound("ANY_CODE"));
        Assert.True(service.HalfPlayersReadyForNextRound("ANOTHER_CODE"));
    }

    [Fact]
    public void StartNextRound_ReturnsError_WhenGameServiceReturnsNull()
    {
        var store = new TournamentStore();

        var p1 = TestHelpers.BuildGuest("A");
        var p2 = TestHelpers.BuildGuest("B");

        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 2,
            CurrentRound = 0,
            TournamentStarted = true,
            Players = new List<User> { p1, p2 },
            GameTypesByRounds = new List<GameType> { GameType.TicTacToe, GameType.RockPaperScissors }
        };
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        gameService.Setup(s => s.StartGame(It.IsAny<GameType>(), It.IsAny<List<User>>()))
           .Returns((IGame?)null);

        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartNextRound("CODE");

        Assert.Equal("Failed to start game for a player group.", error);
        Assert.Empty(session.GamesByPlayers);
        Assert.False(session.RoundStarted);
    }

    [Fact]
    public async Task LogRoundStartAsync_LogsGamesAndUserRounds_Successfully()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");
        var p3 = TestHelpers.BuildGuest("P3");
        var p4 = TestHelpers.BuildGuest("P4");

        var game1 = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: false, winner: null);
        var game2 = new TestGame(GameType.TicTacToe, new List<User> { p3, p4 }, gameOver: false, winner: null);

        var tournamentId = Guid.NewGuid();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = tournamentId,
            NumberOfRounds = 2,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { p1, p2, p3, p4 },
            GameTypesByRounds = new List<GameType> { GameType.TicTacToe, GameType.RockPaperScissors }
        };

        session.GamesByPlayers[p1] = game1;
        session.GamesByPlayers[p2] = game1;
        session.GamesByPlayers[p3] = game2;
        session.GamesByPlayers[p4] = game2;

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.LogRoundStartAsync("CODE");

        using var context = contextFactory.CreateDbContext();
        var games = context.Games.Where(g => g.TournamentId == tournamentId).ToList();
        Assert.Equal(2, games.Count);
        Assert.All(games, g => Assert.Equal(1, g.RoundNumber));
        Assert.All(games, g => Assert.Equal(GameType.TicTacToe.ToString(), g.GameType));

        var userRounds = context.UserRound.ToList();
        Assert.Equal(4, userRounds.Count);
        Assert.Contains(userRounds, ur => ur.UserId == p1.Id);
        Assert.Contains(userRounds, ur => ur.UserId == p2.Id);
        Assert.Contains(userRounds, ur => ur.UserId == p3.Id);
        Assert.Contains(userRounds, ur => ur.UserId == p4.Id);
        Assert.All(userRounds, ur => Assert.Equal(0, ur.PlayerPlacement));
    }

    [Fact]
    public async Task SaveGameResultsAsync_UpdatesPlacements_WithWinner()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");

        var game = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: true, winner: p1);

        var tournamentId = Guid.NewGuid();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = tournamentId,
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { p1, p2 }
        };

        session.GamesByPlayers[p1] = game;
        session.GamesByPlayers[p2] = game;

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);

        // Setup database with game and user rounds
        using (var context = contextFactory.CreateDbContext())
        {
            var gameEntity = new Game
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                GameType = GameType.TicTacToe.ToString(),
                RoundNumber = 1
            };
            context.Games.Add(gameEntity);

            context.UserRound.Add(new UserGame
            {
                UserId = p1.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 1,
                PlayerPlacement = 0
            });

            context.UserRound.Add(new UserGame
            {
                UserId = p2.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 2,
                PlayerPlacement = 0
            });

            context.SaveChanges();
        }

        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.SaveGameResultsAsync("CODE");

        using (var context = contextFactory.CreateDbContext())
        {
            var userRounds = context.UserRound.ToList();
            var p1Round = userRounds.First(ur => ur.UserId == p1.Id);
            var p2Round = userRounds.First(ur => ur.UserId == p2.Id);

            Assert.Equal(1, p1Round.PlayerPlacement);
            Assert.Equal(2, p2Round.PlayerPlacement);
        }
    }

    [Fact]
    public async Task SaveGameResultsAsync_UpdatesPlacements_WithNoWinner()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");

        var game = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: true, winner: null);

        var tournamentId = Guid.NewGuid();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = tournamentId,
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { p1, p2 }
        };

        session.GamesByPlayers[p1] = game;
        session.GamesByPlayers[p2] = game;

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);

        // Setup database with game and user rounds
        using (var context = contextFactory.CreateDbContext())
        {
            var gameEntity = new Game
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                GameType = GameType.TicTacToe.ToString(),
                RoundNumber = 1
            };
            context.Games.Add(gameEntity);

            context.UserRound.Add(new UserGame
            {
                UserId = p1.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 1,
                PlayerPlacement = 0
            });

            context.UserRound.Add(new UserGame
            {
                UserId = p2.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 2,
                PlayerPlacement = 0
            });

            context.SaveChanges();
        }

        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.SaveGameResultsAsync("CODE");

        using (var context = contextFactory.CreateDbContext())
        {
            var userRounds = context.UserRound.ToList();
            Assert.All(userRounds, ur => Assert.Equal(0, ur.PlayerPlacement));
        }
    }

    [Fact]
    public async Task SaveGameResultsAsync_SkipsGamesNotOver()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");

        var game = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: false, winner: null);

        var tournamentId = Guid.NewGuid();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = tournamentId,
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { p1, p2 }
        };

        session.GamesByPlayers[p1] = game;
        session.GamesByPlayers[p2] = game;

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);

        // Setup database with game and user rounds
        using (var context = contextFactory.CreateDbContext())
        {
            var gameEntity = new Game
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                GameType = GameType.TicTacToe.ToString(),
                RoundNumber = 1
            };
            context.Games.Add(gameEntity);

            context.UserRound.Add(new UserGame
            {
                UserId = p1.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 1,
                PlayerPlacement = 0
            });

            context.UserRound.Add(new UserGame
            {
                UserId = p2.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 2,
                PlayerPlacement = 0
            });

            context.SaveChanges();
        }

        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.SaveGameResultsAsync("CODE");

        // Verify no changes were made
        using (var context = contextFactory.CreateDbContext())
        {
            var userRounds = context.UserRound.ToList();
            Assert.All(userRounds, ur => Assert.Equal(0, ur.PlayerPlacement));
        }
    }

    [Fact]
    public async Task CheckAndSaveResultsIfAllGamesEndedAsync_SavesResults_WhenAllGamesEnded()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");

        var game = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: true, winner: p1);

        var tournamentId = Guid.NewGuid();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = tournamentId,
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { p1, p2 }
        };

        session.GamesByPlayers[p1] = game;
        session.GamesByPlayers[p2] = game;

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);

        // Setup database with game and user rounds
        using (var context = contextFactory.CreateDbContext())
        {
            var gameEntity = new Game
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                GameType = GameType.TicTacToe.ToString(),
                RoundNumber = 1
            };
            context.Games.Add(gameEntity);

            context.UserRound.Add(new UserGame
            {
                UserId = p1.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 1,
                PlayerPlacement = 0
            });

            context.UserRound.Add(new UserGame
            {
                UserId = p2.Id,
                GameId = gameEntity.Id,
                PlayerTurn = 2,
                PlayerPlacement = 0
            });

            context.SaveChanges();
        }

        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.CheckAndSaveResultsIfAllGamesEndedAsync("CODE");

        // Verify results were saved
        using (var context = contextFactory.CreateDbContext())
        {
            var userRounds = context.UserRound.ToList();
            var p1Round = userRounds.First(ur => ur.UserId == p1.Id);
            var p2Round = userRounds.First(ur => ur.UserId == p2.Id);

            Assert.Equal(1, p1Round.PlayerPlacement);
            Assert.Equal(2, p2Round.PlayerPlacement);
        }
    }

    [Fact]
    public async Task CheckAndSaveResultsIfAllGamesEndedAsync_DoesNotSave_WhenGamesNotEnded()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");

        var game = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: false, winner: null);

        var tournamentId = Guid.NewGuid();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = tournamentId,
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { p1, p2 }
        };

        session.GamesByPlayers[p1] = game;
        session.GamesByPlayers[p2] = game;

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);

        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.CheckAndSaveResultsIfAllGamesEndedAsync("CODE");

        // Verify no database records were created (no saving occurred)
        using var context = contextFactory.CreateDbContext();
        Assert.Empty(context.Games);
        Assert.Empty(context.UserRound);
    }

    [Fact]
    public void StartTournament_DoesNotAddDuplicate_WhenTournamentExists()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var tournamentId = Guid.NewGuid();
        var store = new TournamentStore();

        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = tournamentId,
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = false,
            Players = new List<User> { TestHelpers.BuildGuest("A"), TestHelpers.BuildGuest("B") }
        };
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);

        // Pre-add the tournament to database
        using (var context = contextFactory.CreateDbContext())
        {
            context.Tournaments.Add(new Tournament { Id = tournamentId });
            context.SaveChanges();
        }

        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartTournament("CODE");

        Assert.Null(error);
        Assert.True(session.TournamentStarted);

        // Verify still only one tournament in database
        using (var context = contextFactory.CreateDbContext())
        {
            var count = context.Tournaments.Count(t => t.Id == tournamentId);
            Assert.Equal(1, count);
        }
    }

    private sealed class TestGame(GameType gameType, List<User> players, bool gameOver, User? winner) : IGame
    {
        public User? Winner { get; private set; } = winner;
        public GameType GameType { get; private set; } = gameType;
        public bool GameOver { get; private set; } = gameOver;
        public List<User> Players { get; private set; } = players;

        public object GetState()
        {
            return new { GameOver };
        }

        public bool MakeMove(System.Text.Json.JsonElement moveData, User player)
        {
            return false;
        }
    }
}
using Api.Data;
using Api.Entities;
using Api.Enums;
using Api.GameLogic;
using Api.Models;
using Api.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

namespace Api.Tests.Services;

public class TournamentServiceUnitTests
{
    [Fact]
    public void GetTournamentSession_ReturnsNull_WhenMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        TournamentSession? session = service.GetTournamentSession("NOPE");

        Assert.Null(session);
    }

    [Fact]
    public void GetTournamentSession_ReturnsSession_WhenExists()
    {
        var store = new TournamentStore();
        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = false
        };
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        TournamentSession? found = service.GetTournamentSession("CODE");

        Assert.Same(session, found);
    }

    [Fact]
    public void GetTournamentRoundInfo_ReturnsDefaults_WhenMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        RoundInfoDto dto = service.GetTournamentRoundInfo("NOPE");

        Assert.Equal(1, dto.CurrentRound);
        Assert.Equal(1, dto.TotalRounds);
    }

    [Fact]
    public void GetTournamentRoundInfo_ClampsValues()
    {
        var store = new TournamentStore();
        store.Sessions["CODE"] = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 0,
            CurrentRound = 99,
            TournamentStarted = false
        };

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        RoundInfoDto dto = service.GetTournamentRoundInfo("CODE");

        Assert.Equal(1, dto.TotalRounds);
        Assert.Equal(1, dto.CurrentRound);
    }

    [Fact]
    public void AreAllGamesEnded_ReturnsFalse_WhenMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        Assert.False(service.AreAllGamesEnded("NOPE"));
    }

    [Fact]
    public void AreAllGamesEnded_ReturnsTrue_WhenAllOver()
    {
        var store = new TournamentStore();
        var p1 = TestHelpers.BuildGuest("P1");
        var p2 = TestHelpers.BuildGuest("P2");

        var game = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: true, winner: p1);
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

        Assert.True(service.AreAllGamesEnded("CODE"));
    }

    [Fact]
    public void AreAllGamesEnded_ReturnsFalse_WhenAnyNotOver()
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

        Assert.False(service.AreAllGamesEnded("CODE"));
    }

    [Fact]
    public void GetGameListForCurrentRound_ReturnsEmpty_WhenMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        Assert.Empty(service.getGameListForCurrentRound("NOPE"));
    }

    [Fact]
    public void RoundStarted_ReturnsSessionValue()
    {
        var store = new TournamentStore();
        store.Sessions["CODE"] = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = false,
            RoundStarted = true
        };

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        Assert.True(service.RoundStarted("CODE"));
        Assert.False(service.RoundStarted("NOPE"));
    }

    [Fact]
    public void TournamentStarted_ReturnsSessionValue()
    {
        var store = new TournamentStore();
        store.Sessions["CODE"] = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = true,
            RoundStarted = false
        };

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        Assert.True(service.TournamentStarted("CODE"));
        Assert.False(service.TournamentStarted("NOPE"));
    }

    [Fact]
    public void GetTargetGroup_ReturnsNull_WhenMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        Assert.Null(service.getTargetGroup(TestHelpers.BuildGuest("U"), "NOPE"));
    }

    [Fact]
    public void GetTargetGroup_ReturnsPlayers_WhenFoundByUserId()
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

        User otherInstanceSameId = new Guest { Id = p1.Id, Name = "Other" };

        List<User>? group = service.getTargetGroup(otherInstanceSameId, "CODE");

        Assert.NotNull(group);
        Assert.Equal(2, group.Count);
        Assert.Contains(group, u => u.Id == p1.Id);
        Assert.Contains(group, u => u.Id == p2.Id);
    }

    [Fact]
    public void GetGame_ReturnsTrue_WhenUsingSameUserInstance()
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
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        var ok = service.GetGame("CODE", p1, out IGame? found);

        Assert.True(ok);
        Assert.Same(game, found);
    }

    [Fact]
    public void GetGame_ReturnsFalse_WhenDifferentUserInstance()
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
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        User sameIdDifferentInstance = new Guest { Id = p1.Id, Name = "P1" };
        var ok = service.GetGame("CODE", sameIdDifferentInstance, out IGame? found);

        Assert.False(ok);
        Assert.Null(found);
    }

    [Fact]
    public void StartTournament_ReturnsError_WhenSessionMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");

        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartTournament("NOPE");

        Assert.Equal("Tournament session not found.", error);
    }

    [Fact]
    public void StartTournament_ReturnsError_WhenNotEnoughPlayers()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = false,
            Players = new List<User> { TestHelpers.BuildGuest("Solo") }
        };
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartTournament("CODE");

        Assert.Equal("Not enough players to start the tournament.", error);
        Assert.False(session.TournamentStarted);

        using DatabaseContext context = contextFactory.CreateDbContext();
        Assert.Empty(context.Tournaments);
    }

    [Fact]
    public void StartTournament_SetsStarted_AndAddsTournament_WhenMissing()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = false,
            Players = new List<User> { TestHelpers.BuildGuest("A"), TestHelpers.BuildGuest("B") }
        };
        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartTournament("CODE");

        Assert.Null(error);
        Assert.True(session.TournamentStarted);

        using DatabaseContext context = contextFactory.CreateDbContext();
        Assert.True(context.Tournaments.Any(t => t.Id == session.TournamentId));
    }

    [Fact]
    public void StartNextRound_ReturnsError_WhenSessionMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartNextRound("NOPE");

        Assert.Equal("Tournament session not found.", error);
    }

    [Fact]
    public void StartNextRound_ReturnsError_WhenCompleted()
    {
        var store = new TournamentStore();
        store.Sessions["CODE"] = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            Players = new List<User> { TestHelpers.BuildGuest("A"), TestHelpers.BuildGuest("B") },
            GameTypesByRounds = new List<GameType> { GameType.TicTacToe }
        };

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartNextRound("CODE");

        Assert.Equal("All tournament rounds have been completed.", error);
    }

    [Fact]
    public void StartNextRound_ReturnsError_WhenNotEnoughPlayers()
    {
        var store = new TournamentStore();
        store.Sessions["CODE"] = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 2,
            CurrentRound = 0,
            TournamentStarted = true,
            Players = new List<User> { TestHelpers.BuildGuest("Solo") },
            GameTypesByRounds = new List<GameType> { GameType.TicTacToe }
        };

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartNextRound("CODE");

        Assert.Equal("Not enough players to start the next round.", error);
    }

    [Fact]
    public void StartNextRound_ReturnsError_WhenNoGameTypeConfigured()
    {
        var store = new TournamentStore();
        store.Sessions["CODE"] = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 2,
            CurrentRound = 0,
            TournamentStarted = true,
            Players = new List<User> { TestHelpers.BuildGuest("A"), TestHelpers.BuildGuest("B") },
            GameTypesByRounds = new List<GameType>()
        };

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartNextRound("CODE");

        Assert.Equal("No game type configured for round 1.", error);
    }

    [Fact]
    public void StartNextRound_StartsGameAndAdvancesRound()
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

        var startedGame = new TestGame(GameType.TicTacToe, new List<User> { p1, p2 }, gameOver: false, winner: null);
        var gameService = new Mock<IGameService>();
        gameService.Setup(s => s.StartGame(GameType.TicTacToe, It.Is<List<User>>(l => l.Count == 2 && l.Contains(p1) && l.Contains(p2))))
           .Returns(startedGame);

        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        var error = service.StartNextRound("CODE");

        Assert.Null(error);
        Assert.Equal(1, session.CurrentRound);
        Assert.True(session.RoundStarted);
        Assert.Equal(2, session.GamesByPlayers.Count);
        Assert.Same(startedGame, session.GamesByPlayers[p1]);
        Assert.Same(startedGame, session.GamesByPlayers[p2]);
        gameService.VerifyAll();
    }

    [Fact]
    public async Task LogRoundStartAsync_DoesNothing_WhenSessionMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.LogRoundStartAsync("NOPE");
    }

    [Fact]
    public async Task LogRoundStartAsync_Returns_WhenGameTypeIndexInvalid()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 0,
            TournamentStarted = true,
            Players = new List<User> { TestHelpers.BuildGuest("A"), TestHelpers.BuildGuest("B") },
            GameTypesByRounds = new List<GameType> { GameType.TicTacToe }
        };

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.LogRoundStartAsync("CODE");
    }

    [Fact]
    public async Task SaveGameResultsAsync_DoesNothing_WhenSessionMissing()
    {
        var store = new TournamentStore();
        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory($"Ts_{Guid.NewGuid()}");
        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.SaveGameResultsAsync("NOPE");
    }

    [Fact]
    public async Task SaveGameResultsAsync_DoesNothing_WhenNoGamesInDbForRound()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = true,
            Players = new List<User> { TestHelpers.BuildGuest("A"), TestHelpers.BuildGuest("B") }
        };

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.SaveGameResultsAsync("CODE");
    }

    [Fact]
    public async Task CheckAndSaveResultsIfAllGamesEndedAsync_DoesNothing_WhenRoundNotStarted()
    {
        var dbName = $"Ts_{Guid.NewGuid()}";
        var store = new TournamentStore();

        var session = new TournamentSession
        {
            Code = "CODE",
            TournamentId = Guid.NewGuid(),
            NumberOfRounds = 1,
            CurrentRound = 1,
            TournamentStarted = true,
            RoundStarted = false,
            Players = new List<User> { TestHelpers.BuildGuest("A"), TestHelpers.BuildGuest("B") }
        };

        store.Sessions["CODE"] = session;

        var gameService = new Mock<IGameService>();
        IDbContextFactory<DatabaseContext> contextFactory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var service = new TournamentService(gameService.Object, store, contextFactory);

        await service.CheckAndSaveResultsIfAllGamesEndedAsync("CODE");
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

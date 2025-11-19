using Api.Services;
using Api.Entities;
using Api.GameLogic;
using System.Text.Json;

namespace Api.Tests.Integration;

public class EndToEndGameTests
{
    [Fact]
    public async Task CompleteGameFlow_TwoPlayers_TicTacToe()
    {
        var gameFactory = new GameFactory();
        var gameService = new GameService(gameFactory);
        var lobbyService = new LobbyService(gameFactory);
        
        var code = await lobbyService.CreateLobbyWithSettings(2, 1, false, new List<string> { "TicTacToe" });
        
        var player1 = new Guest { Id = Guid.NewGuid(), Name = "Player1" };
        var player2 = new Guest { Id = Guid.NewGuid(), Name = "Player2" };
        
        await lobbyService.JoinMatch(code, player1);
        await lobbyService.JoinMatch(code, player2);
        
        var gameId = $"{code}_G0_R0";
        var players = new List<User> { player1, player2 };
        
        var gameStarted = gameService.StartGame(gameId, "TicTacToe", players);
        Assert.True(gameStarted);
        
        var move1 = JsonSerializer.Serialize(new { PlayerId = player1.Id, X = 0, Y = 0 });
        var elem1 = JsonDocument.Parse(move1).RootElement;
        Assert.True(gameService.MakeMove(gameId, elem1, out var state1));
        
        var move2 = JsonSerializer.Serialize(new { PlayerId = player2.Id, X = 1, Y = 0 });
        var elem2 = JsonDocument.Parse(move2).RootElement;
        Assert.True(gameService.MakeMove(gameId, elem2, out var state2));
        
        var move3 = JsonSerializer.Serialize(new { PlayerId = player1.Id, X = 0, Y = 1 });
        var elem3 = JsonDocument.Parse(move3).RootElement;
        Assert.True(gameService.MakeMove(gameId, elem3, out var state3));
        
        var move4 = JsonSerializer.Serialize(new { PlayerId = player2.Id, X = 1, Y = 1 });
        var elem4 = JsonDocument.Parse(move4).RootElement;
        Assert.True(gameService.MakeMove(gameId, elem4, out var state4));
        
        var move5 = JsonSerializer.Serialize(new { PlayerId = player1.Id, X = 0, Y = 2 });
        var elem5 = JsonDocument.Parse(move5).RootElement;
        Assert.True(gameService.MakeMove(gameId, elem5, out var finalState));
        
        var winner = finalState!.GetType().GetProperty("Winner")!.GetValue(finalState) as string;
        Assert.Equal("Player1", winner);
        Assert.Equal(1, player1.Wins);
    }

    [Fact]
    public async Task GameService_StartGame_And_GetState()
    {
        var factory = new GameFactory();
        var gameService = new GameService(factory);
        
        var player1 = new Guest { Id = Guid.NewGuid(), Name = "Alice" };
        var player2 = new Guest { Id = Guid.NewGuid(), Name = "Bob" };
        var players = new List<User> { player1, player2 };
        
        var gameId = "TEST_GAME_123";
        var started = gameService.StartGame(gameId, "ConnectFour", players);
        Assert.True(started);
        
        var state = gameService.GetGameState(gameId);
        
        Assert.NotNull(state);
        var board = state!.GetType().GetProperty("Board")?.GetValue(state);
        Assert.NotNull(board);
        
        var removed = gameService.RemoveGame(gameId);
        Assert.True(removed);
    }
    
    [Fact]
    public async Task LobbyService_JoinAndLeave_Players()
    {
        var factory = new GameFactory();
        var lobbyService = new LobbyService(factory);
        var code = await lobbyService.CreateLobbyWithSettings(3, 1, true, null);
        
        var player1 = new Guest { Id = Guid.NewGuid(), Name = "P1" };
        var player2 = new Guest { Id = Guid.NewGuid(), Name = "P2" };
        
        var join1 = await lobbyService.JoinMatch(code, player1);
        var join2 = await lobbyService.JoinMatch(code, player2);
        
        Assert.Null(join1);
        Assert.Null(join2);
        
        var players = lobbyService.GetPlayersInLobby(code);
        Assert.Equal(2, players.Count);
        
        var left = await lobbyService.LeaveMatch(code, player1.Id);
        Assert.True(left);
        
        var remainingPlayers = lobbyService.GetPlayersInLobby(code);
        Assert.Single(remainingPlayers);
    }
}
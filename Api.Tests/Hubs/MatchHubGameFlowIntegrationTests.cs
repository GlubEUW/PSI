using System.Net.Http.Json;
using System.Text.Json;

using Api.GameLogic;
using Api.Models;
using Api.Tests.TestServer;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Api.Tests.Hubs;

public class MatchHubGameFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
   private readonly CustomWebApplicationFactory _factory;
   public MatchHubGameFlowIntegrationTests(CustomWebApplicationFactory factory)
   {
      _factory = factory;
   }

   private static void AddAuthHeaders(HttpClient client, Guid id, string name, string role = "Guest")
   {
      client.DefaultRequestHeaders.Remove("X-Test-UserId");
      client.DefaultRequestHeaders.Remove("X-Test-Name");
      client.DefaultRequestHeaders.Remove("X-Test-Role");
      client.DefaultRequestHeaders.Add("X-Test-UserId", id.ToString());
      client.DefaultRequestHeaders.Add("X-Test-Name", name);
      client.DefaultRequestHeaders.Add("X-Test-Role", role);
   }

   private HubConnection BuildConnection(string url, Guid id, string name, string role = "Guest")
   {
      return new HubConnectionBuilder()
         .WithUrl(url, options =>
         {
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
            options.Headers.Add("X-Test-UserId", id.ToString());
            options.Headers.Add("X-Test-Name", name);
            options.Headers.Add("X-Test-Role", role);
         })
         .Build();
   }

   private static async Task<(string code, HttpClient client)> CreateLobbyAsync(CustomWebApplicationFactory factory)
   {
      var client = factory.CreateClient();
      // We'll set Alice as the creator
      var aliceId = Guid.NewGuid();
      AddAuthHeaders(client, aliceId, "Alice");
      var req = new CreateLobbyDto
      {
         NumberOfPlayers = 2,
         NumberOfRounds = 1,
         RandomGames = false,
         GamesList = new List<string> { "RockPaperScissors" }
      };
      var resp = await client.PostAsJsonAsync("api/Lobby/create", req);
      resp.EnsureSuccessStatusCode();
      using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
      var code = doc.RootElement.GetProperty("code").GetString()!;
      return (code, client);
   }

   [Fact]
   public async Task StartMatch_HappyPath_SendsMatchStarted_ToBothPlayers()
   {
      var (code, _) = await CreateLobbyAsync(_factory);
      var url = _factory.Server.BaseAddress + $"matchHub?code={code}";

      var aliceId = Guid.NewGuid();
      var bobId = Guid.NewGuid();

      var connA = BuildConnection(url, aliceId, "Alice");
      var connB = BuildConnection(url, bobId, "Bob");

      var tcsA = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      var tcsB = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

      connA.On<JsonElement>("MatchStarted", payload => tcsA.TrySetResult(payload));
      connB.On<JsonElement>("MatchStarted", payload => tcsB.TrySetResult(payload));

      await connA.StartAsync();
      await connB.StartAsync();

      // Trigger start from Alice
      await connA.InvokeAsync("StartMatch");

      var completedA = await Task.WhenAny(tcsA.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      var completedB = await Task.WhenAny(tcsB.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(tcsA.Task.IsCompleted, "Alice should receive MatchStarted");
      Assert.True(tcsB.Task.IsCompleted, "Bob should receive MatchStarted");

      var pA = await tcsA.Task;
      var pB = await tcsB.Task;
      Assert.Equal("RockPaperScissors", pA.GetProperty("gameType").GetString());
      Assert.Equal("RockPaperScissors", pB.GetProperty("gameType").GetString());
      Assert.Equal(1, pA.GetProperty("round").GetInt32());

      await connA.DisposeAsync();
      await connB.DisposeAsync();
   }

   [Fact]
   public async Task MakeMove_Then_EndGame_SendsGameUpdate_And_RoundEnded()
   {
      var (code, _) = await CreateLobbyAsync(_factory);
      var url = _factory.Server.BaseAddress + $"matchHub?code={code}";

      var aliceId = Guid.NewGuid();
      var bobId = Guid.NewGuid();

      var connA = BuildConnection(url, aliceId, "Alice");
      var connB = BuildConnection(url, bobId, "Bob");

      var startedA = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      var startedB = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

      connA.On<JsonElement>("MatchStarted", payload => startedA.TrySetResult(payload));
      connB.On<JsonElement>("MatchStarted", payload => startedB.TrySetResult(payload));

      var updateA = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      var updateB = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      connA.On<JsonElement>("GameUpdate", payload => updateA.TrySetResult(payload));
      connB.On<JsonElement>("GameUpdate", payload => updateB.TrySetResult(payload));

      var roundEnded = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      // Both connections are in the lobby group (code), so either one can receive PlayersUpdated/RoundEnded
      connA.On<JsonElement>("RoundEnded", payload => roundEnded.TrySetResult(payload));

      await connA.StartAsync();
      await connB.StartAsync();

      await connA.InvokeAsync("StartMatch");
      await Task.WhenAll(
         Task.WhenAny(startedA.Task, Task.Delay(TimeSpan.FromSeconds(5))),
         Task.WhenAny(startedB.Task, Task.Delay(TimeSpan.FromSeconds(5)))
      );
      Assert.True(startedA.Task.IsCompleted && startedB.Task.IsCompleted, "Both players should receive MatchStarted");

      var msA = await startedA.Task;
      var gameId = msA.GetProperty("gameId").GetString()!;

      // Alice makes a move: Rock
      var moveA = JsonSerializer.SerializeToElement(new RockPaperScissorsMove { PlayerId = aliceId, Choice = RockPaperScissorsChoice.Rock });
      await connA.InvokeAsync("MakeMove", moveA);
      await Task.WhenAny(updateA.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(updateA.Task.IsCompleted, "Alice should receive a GameUpdate after her move");

      // Bob makes a move: Scissors
      var moveB = JsonSerializer.SerializeToElement(new RockPaperScissorsMove { PlayerId = bobId, Choice = RockPaperScissorsChoice.Scissors });
      await connB.InvokeAsync("MakeMove", moveB);
      await Task.WhenAny(updateB.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(updateB.Task.IsCompleted, "Bob should receive a GameUpdate after his move");

      // End the game; expect RoundEnded broadcast to lobby group
      await connA.InvokeAsync("EndGame", gameId);
      await Task.WhenAny(roundEnded.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(roundEnded.Task.IsCompleted, "RoundEnded should be sent when all games ended");

      await connA.DisposeAsync();
      await connB.DisposeAsync();
   }
}

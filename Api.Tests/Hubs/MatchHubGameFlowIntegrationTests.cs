using System.Net.Http.Json;
using System.Text.Json;

using Api.GameLogic;
using Api.Models;
using Api.Tests.TestServer;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Api.Tests.Hubs;

public class TournamentHubGameFlowIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
   private readonly CustomWebApplicationFactory _factory = factory;

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
      var Player1Id = Guid.NewGuid();
      AddAuthHeaders(client, Player1Id, "Player1");
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
      var url = _factory.Server.BaseAddress + $"TournamentHub?code={code}";

      var Player1Id = Guid.NewGuid();
      var Player2Id = Guid.NewGuid();

      var connA = BuildConnection(url, Player1Id, "Player1");
      var connB = BuildConnection(url, Player2Id, "Player2");

      var tcsA = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      var tcsB = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

      connA.On<JsonElement>("MatchStarted", payload => tcsA.TrySetResult(payload));
      connB.On<JsonElement>("MatchStarted", payload => tcsB.TrySetResult(payload));

      await connA.StartAsync();
      await connB.StartAsync();

      await connA.InvokeAsync("StartMatch");

      var completedA = await Task.WhenAny(tcsA.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      var completedB = await Task.WhenAny(tcsB.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(tcsA.Task.IsCompleted, "Player1 should receive MatchStarted");
      Assert.True(tcsB.Task.IsCompleted, "Player2 should receive MatchStarted");

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
      var url = _factory.Server.BaseAddress + $"TournamentHub?code={code}";

      var Player1Id = Guid.NewGuid();
      var Player2Id = Guid.NewGuid();

      var connA = BuildConnection(url, Player1Id, "Player1");
      var connB = BuildConnection(url, Player2Id, "Player2");

      var startedA = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      var startedB = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

      connA.On<JsonElement>("MatchStarted", payload => startedA.TrySetResult(payload));
      connB.On<JsonElement>("MatchStarted", payload => startedB.TrySetResult(payload));

      var updateA = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      var updateB = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      connA.On<JsonElement>("GameUpdate", payload => updateA.TrySetResult(payload));
      connB.On<JsonElement>("GameUpdate", payload => updateB.TrySetResult(payload));

      var roundEnded = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
      connA.On<JsonElement>("RoundEnded", payload => roundEnded.TrySetResult(payload));

      // Ensure both clients have fully joined the lobby (PlayersUpdated is sent on connect)
      var lobbyA = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var lobbyB = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      connA.On<RoundInfoDto>("PlayersUpdated", _ => lobbyA.TrySetResult(true));
      connB.On<RoundInfoDto>("PlayersUpdated", _ => lobbyB.TrySetResult(true));

      await connA.StartAsync();
      await connB.StartAsync();

      await Task.WhenAll(
         Task.WhenAny(lobbyA.Task, Task.Delay(TimeSpan.FromSeconds(10))),
         Task.WhenAny(lobbyB.Task, Task.Delay(TimeSpan.FromSeconds(10)))
      );

      // Robust start: retry StartMatch if lobby reports not all players yet
      var startErrors = new List<string>();
      var errorTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
      connA.On<string>("Error", msg =>
      {
         // capture transient lobby state errors
         if (!errorTcs.Task.IsCompleted)
            errorTcs.TrySetResult(msg);
      });

      for (var attempt = 0; attempt < 3 && (!startedA.Task.IsCompleted || !startedB.Task.IsCompleted); attempt++)
      {
         await connA.InvokeAsync("StartMatch");
         var wait = Task.Delay(TimeSpan.FromSeconds(5));
         await Task.WhenAll(
            Task.WhenAny(startedA.Task, wait),
            Task.WhenAny(startedB.Task, wait)
         );
         if (!startedA.Task.IsCompleted || !startedB.Task.IsCompleted)
         {
            if (errorTcs.Task.IsCompleted)
            {
               // Await the already completed task instead of using .Result
               startErrors.Add(await errorTcs.Task);
               errorTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            await Task.Delay(500);
         }
      }

      await Task.WhenAll(
         Task.WhenAny(startedA.Task, Task.Delay(TimeSpan.FromSeconds(10))),
         Task.WhenAny(startedB.Task, Task.Delay(TimeSpan.FromSeconds(10)))
      );
      Assert.True(startedA.Task.IsCompleted && startedB.Task.IsCompleted, "Both players should receive MatchStarted");

      var msA = await startedA.Task;
      var gameId = msA.GetProperty("gameId").GetString()!;

      var moveA = JsonSerializer.SerializeToElement(new RockPaperScissorsMove { PlayerId = Player1Id, Choice = RockPaperScissorsChoice.Rock });
      await connA.InvokeAsync("MakeMove", moveA);
      await Task.WhenAny(updateA.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(updateA.Task.IsCompleted, "Player1 should receive a GameUpdate after her move");

      var moveB = JsonSerializer.SerializeToElement(new RockPaperScissorsMove { PlayerId = Player2Id, Choice = RockPaperScissorsChoice.Scissors });
      await connB.InvokeAsync("MakeMove", moveB);
      await Task.WhenAny(updateB.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(updateB.Task.IsCompleted, "Player2 should receive a GameUpdate after his move");

      await connA.InvokeAsync("EndGame", gameId);
      await Task.WhenAny(roundEnded.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(roundEnded.Task.IsCompleted, "RoundEnded should be sent when all games ended");

      await connA.DisposeAsync();
      await connB.DisposeAsync();
   }
}

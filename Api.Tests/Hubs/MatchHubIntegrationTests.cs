using System.Net.Http.Json;
using System.Text.Json;

using Api.Models;
using Api.Tests.TestServer;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Api.Tests.Hubs;

public class MatchHubIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
   private readonly CustomWebApplicationFactory _factory = factory;

   private static void AddAuthHeaders(HttpClient client, Guid? id = null, string name = "Player1", string role = "Guest")
   {
      var userId = (id ?? Guid.NewGuid()).ToString();
      client.DefaultRequestHeaders.Remove("X-Test-UserId");
      client.DefaultRequestHeaders.Remove("X-Test-Name");
      client.DefaultRequestHeaders.Remove("X-Test-Role");
      client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
      client.DefaultRequestHeaders.Add("X-Test-Name", name);
      client.DefaultRequestHeaders.Add("X-Test-Role", role);
   }

   private HubConnection BuildConnection(string url, Guid? id = null, string name = "Player1", string role = "Guest")
   {
      var userId = (id ?? Guid.NewGuid()).ToString();
      return new HubConnectionBuilder()
         .WithUrl(url, options =>
         {
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
            options.Headers.Add("X-Test-UserId", userId);
            options.Headers.Add("X-Test-Name", name);
            options.Headers.Add("X-Test-Role", role);
         })
         .Build();
   }

   [Fact]
   public async Task Connect_WithoutCode_SendsErrorAndCloses()
   {
      var connection = BuildConnection(_factory.Server.BaseAddress + "matchHub");
      var tcsError = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

      connection.On<string>("Error", msg => tcsError.TrySetResult(msg));

      await connection.StartAsync();
      await Task.WhenAny(tcsError.Task, Task.Delay(TimeSpan.FromSeconds(3)));
      Assert.True(tcsError.Task.IsCompleted, "Expected Error message before timeout");
      Assert.Equal("Invalid connection parameters.", await tcsError.Task);

      await connection.DisposeAsync();
   }

   [Fact]
   public async Task Connect_WithValidCode_ReceivesPlayersUpdated()
   {

      var client = _factory.CreateClient();
      AddAuthHeaders(client, name: "Player1");
      var req = new CreateLobbyDto { NumberOfPlayers = 2, NumberOfRounds = 1, RandomGames = true };
      var resp = await client.PostAsJsonAsync("api/Lobby/create", req);
      resp.EnsureSuccessStatusCode();
      using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
      var code = doc.RootElement.GetProperty("code").GetString();
      Assert.False(string.IsNullOrWhiteSpace(code));

      var url = _factory.Server.BaseAddress + $"matchHub?code={code}";
      var connection = BuildConnection(url, name: "Player1");
      var tcsPlayers = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

      connection.On<JsonElement>("PlayersUpdated", payload => tcsPlayers.TrySetResult(payload));

      await connection.StartAsync();
      var completed = await Task.WhenAny(tcsPlayers.Task, Task.Delay(TimeSpan.FromSeconds(5)));
      Assert.True(tcsPlayers.Task.IsCompleted, "Expected PlayersUpdated before timeout");

      await connection.DisposeAsync();
   }
}

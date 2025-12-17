using System.Net.Http.Json;
using System.Text.Json;

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
}

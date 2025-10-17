namespace Api.Models;
public record class LobbyInfoDto
{
   public List<string> Players { get; set; } = new();
}
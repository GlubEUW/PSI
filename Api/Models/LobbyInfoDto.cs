namespace Api.Models;
public record LobbyInfoDto
{
   public List<(string, Guid)> Players { get; set; } = new();
}
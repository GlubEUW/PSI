namespace Api.Models;

public class CreateLobbyDto
{
   public int NumberOfRounds { get; set; } = 1;
   public List<string> GamesList { get; set; } = new();
   public int MaxPlayers { get; set; } = 2;
}
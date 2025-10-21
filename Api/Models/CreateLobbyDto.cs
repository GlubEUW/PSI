namespace Api.Models;

public class CreateLobbyDto
{
   public int NumberOfPlayers { get; set; } = 2;
   public int NumberOfRounds { get; set; } = 1;
   public bool RandomGames { get; set; } = false;
   public List<string>? GamesList { get; set; } = null;
}
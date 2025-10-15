namespace Api.Models;

public record class LobbyInfoDto
{
   public bool IsLobbyFull { get; set; }
   public bool IsNameTakenInLobby { get; set; }
   public List<string> Players { get; set; } = new();
}

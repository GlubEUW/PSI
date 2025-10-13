namespace Api.Models;

public record class LobbyInfoDto
{
   public bool IsLobbyFull { get; set; } = false;
   public bool IsNameTakenInLobby { get; set; } = false;
}

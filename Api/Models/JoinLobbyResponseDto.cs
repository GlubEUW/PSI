namespace Api.Models;

public record class JoinLobbyResponseDto
{
   public bool IsLobbyFull { get; set; }
   public bool IsNameTakenInLobby { get; set; }
   public List<string> Players { get; set; } = new();
}

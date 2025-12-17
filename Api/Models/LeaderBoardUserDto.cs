namespace Api.Models;

public class LeaderBoardUserDto
{
   public Guid Id { get; set; }
   public string Name { get; set; } = string.Empty;
   public int TotalWins { get; set; }

   public int TicTacToeWins { get; set; }
   public int RockPaperScissorsWins { get; set; }
   public int ConnectFourWins { get; set; }
}

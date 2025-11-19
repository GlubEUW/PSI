using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Api.Models;

public class GameStatsDto
{
   [JsonIgnore]
   public Guid UserId { get; set; }
   [NotMapped]
   public string Name { get; set; } = string.Empty;
   public int TotalWins { get; set; }
   public int TotalGamesPlayed { get; set; }

   // One pair of columns per GameType
   public int TicTacToeWins { get; set; } = 0;
   public int TicTacToeGamesPlayed { get; set; } = 0;

   public int RockPaperScissorsWins { get; set; } = 0;
   public int RockPaperScissorsGamesPlayed { get; set; } = 0;
}
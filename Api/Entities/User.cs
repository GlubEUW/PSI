using System.ComponentModel.DataAnnotations.Schema;

using Api.Enums;
using Api.Models;

namespace Api.Entities;

public abstract class User : IComparable<User> // Usage of standard DOTNET interface
{
   public Guid Id { get; set; } = Guid.Empty;
   public string Name { get; set; } = string.Empty;
   public int Wins { get; set; } = 0;
   [NotMapped]
   public Dictionary<GameType, GameStats> PlayedAndWonGamesByType { get; set; }
      = Enum.GetValues<GameType>()
         .ToDictionary(gt => gt, gt => new GameStats());

   public class GameStats
   {
      public int Wins { get; set; } = 0;
      public int GamesPlayed { get; set; } = 0;
   }

   public GameStatsDto ToGameStatsDto()
   {
      return new GameStatsDto
      {
         UserId = Id,
         TotalWins = PlayedAndWonGamesByType.Values.Sum(gs => gs.Wins),
         TotalGamesPlayed = PlayedAndWonGamesByType.Values.Sum(gs => gs.GamesPlayed),
         TicTacToeWins = PlayedAndWonGamesByType[GameType.TicTacToe].Wins,
         TicTacToeGamesPlayed = PlayedAndWonGamesByType[GameType.TicTacToe].GamesPlayed,
         RockPaperScissorsWins = PlayedAndWonGamesByType[GameType.RockPaperScissors].Wins,
         RockPaperScissorsGamesPlayed = PlayedAndWonGamesByType[GameType.RockPaperScissors].GamesPlayed,
         ConnectFourWins = PlayedAndWonGamesByType[GameType.ConnectFour].Wins,
         ConnectFourGamesPlayed = PlayedAndWonGamesByType[GameType.ConnectFour].GamesPlayed
      };
   }

   public void LoadFromGameStatsDto(GameStatsDto data)
   {
      PlayedAndWonGamesByType[GameType.TicTacToe].Wins = data.TicTacToeWins;
      PlayedAndWonGamesByType[GameType.TicTacToe].GamesPlayed = data.TicTacToeGamesPlayed;
      PlayedAndWonGamesByType[GameType.RockPaperScissors].Wins = data.RockPaperScissorsWins;
      PlayedAndWonGamesByType[GameType.RockPaperScissors].GamesPlayed = data.RockPaperScissorsGamesPlayed;
      PlayedAndWonGamesByType[GameType.ConnectFour].Wins = data.ConnectFourWins;
      PlayedAndWonGamesByType[GameType.ConnectFour].GamesPlayed = data.ConnectFourGamesPlayed;
   }

   public int CompareTo(User? other) // FIXME: Compare data from database, not saved in user during session
   {
      if (other is null)
         return 1;

      return Wins.CompareTo(other.Wins);
   }
}

public class Guest : User { }

public class RegisteredUser : User
{
   public string PasswordHash { get; set; } = string.Empty;
}

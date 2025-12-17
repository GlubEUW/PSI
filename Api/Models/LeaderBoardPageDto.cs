namespace Api.Models;

public class LeaderBoardPageDto
{
   public int Page { get; set; }
   public int PageSize { get; set; }
   public int TotalUsers { get; set; }
   public List<LeaderBoardUserDto> Users { get; set; } = new();
}

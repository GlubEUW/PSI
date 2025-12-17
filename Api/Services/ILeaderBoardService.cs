using Api.Models;

namespace Api.Services;

public interface ILeaderBoardService
{
   public Task<LeaderBoardPageDto> GetLeaderBoardAsync(int page, int pageSize);
}

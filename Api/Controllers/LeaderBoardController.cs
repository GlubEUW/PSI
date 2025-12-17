using Microsoft.AspNetCore.Mvc;

using Api.Models;
using Api.Services;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LeaderBoardController(ILeaderBoardService leaderBoardService) : ControllerBase
{
   [HttpGet]
   public async Task<ActionResult<LeaderBoardPageDto>> Get([FromQuery] int page = 0, [FromQuery] int pageSize = 100)
   {
      LeaderBoardPageDto result = await leaderBoardService.GetLeaderBoardAsync(page, pageSize);
      return Ok(result);
   }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Services;

namespace Api.Controllers
{
   [Authorize]
   [Route("api/[controller]")]
   [ApiController]
   public class LobbyController : ControllerBase
   {
      private readonly ILobbyService _lobbyService;

      public LobbyController(ILobbyService lobbyService)
      {
         _lobbyService = lobbyService;
      }

      [HttpPost("{code}/create")]
      public async Task<ActionResult> CreateMatch(string code)
      {
         var success = await _lobbyService.CreateMatch(code);
         if (success)
            return Ok(new { Message = $"Match {code} created." });

         return Conflict(new { Message = $"Match {code} already exists." });
      }

      [HttpPost("{code}/canjoin")]
      public async Task<ActionResult> CanJoinMatch(string code)
      {
         var name = User.Identity?.Name;
         if (name is null)
            return Unauthorized();

         var error = _lobbyService.CanJoinLobby(code, name);
         if (error is null)
            return Ok(new { Message = "Can join match." });
         return BadRequest(new { Message = error });
      }

      [HttpPost("{code}/leave")]
      public async Task<ActionResult> LeaveMatch(string code)
      {
         var name = User.Identity?.Name;
         if (name is null)
            return Unauthorized();

         var success = await _lobbyService.LeaveMatch(code, name);
         if (!success)
            return BadRequest(new { Message = "Unable to leave match or match does not exist." });

         return Ok(new { Message = $"Left match {code} successfully." });
      }
   }
}

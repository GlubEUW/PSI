using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Models;
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

      [HttpPost("{code}/join")]
      public async Task<ActionResult> JoinMatch(string code)
      {
         var name = User.Identity?.Name;
         if (name is null) return Unauthorized();

         var success = await _lobbyService.JoinMatch(code, name);
         if (!success)
         {
            if (_lobbyService.IsLobbyFull(code))
               return Conflict(new { Message = "Match is full." });

            if (_lobbyService.IsNameTakenInLobby(code, name))
               return Conflict(new { Message = "Name already taken in match." });

            return BadRequest(new { Message = "Unable to join match." });
         }

         return Ok(new { Message = $"Joined match {code} successfully." });
      }

      [HttpPost("{code}/leave")]
      public async Task<ActionResult> LeaveMatch(string code)
      {
         var name = User.Identity?.Name;
         if (name is null) return Unauthorized();

         var success = await _lobbyService.LeaveMatch(code, name);
         if (!success)
            return BadRequest(new { Message = "Unable to leave match or match does not exist." });

         return Ok(new { Message = $"Left match {code} successfully." });
      }
   }
}

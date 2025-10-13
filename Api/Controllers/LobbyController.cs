using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Models;
using Api.Services;

namespace Api.Controllers
{
   [Route("api/[controller]")]
   [ApiController]
   public class LobbyController : ControllerBase
   {
      private readonly ILobbyService _lobbyService;

      public LobbyController(ILobbyService lobbyService)
      {
         _lobbyService = lobbyService;
      }

      [Authorize]
      [HttpGet("{code}")]
      public ActionResult<LobbyInfoDto> GetLobbyInfo(string code)
      {
         var name = User.Identity?.Name;
         var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
         if (name is null || idClaim is null || !Guid.TryParse(idClaim.Value, out _))
            return Unauthorized();

         var lobbyInfo = _lobbyService.GetLobbyInfo(code, name);
         return Ok(lobbyInfo);
      }

      [Authorize]
      [HttpPost("{code}/create")]
      public async Task<ActionResult> CreateMatch(string code)
      {
         var success = _lobbyService.CreateMatch(code);
         if (success)
            return Ok(new { Message = $"Match {code} created." });

         return Conflict(new { Message = $"Match {code} already exists." });
      }

      [Authorize]
      [HttpPost("{code}/join")]
      public async Task<ActionResult> JoinMatch(string code)
      {
         var name = User.Identity?.Name;
         if (name is null) return Unauthorized();

         var success = _lobbyService.JoinMatch(code, name);
         if (!success)
         {
            var lobbyInfo = _lobbyService.GetLobbyInfo(code, name);
            if (lobbyInfo.IsLobbyFull)
               return Conflict(new { Message = "Match is full." });

            if (lobbyInfo.IsNameTakenInLobby)
               return Conflict(new { Message = "Name already taken in match." });

            return BadRequest(new { Message = "Unable to join match." });
         }

         return Ok(new { Message = $"Joined match {code} successfully." });
      }


      [Authorize]
      [HttpPost("{code}/leave")]
      public async Task<ActionResult> LeaveMatch(string code)
      {
         var name = User.Identity?.Name;
         if (name is null) return Unauthorized();

         var success = _lobbyService.LeaveMatch(code, name);
         if (!success)
            return BadRequest(new { Message = "Unable to leave match or match does not exist." });

         return Ok(new { Message = $"Left match {code} successfully." });
      }
   }
}

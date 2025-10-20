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
      [HttpPost("create")]
      public async Task<ActionResult> CreateLobbyWithSettings([FromBody] CreateLobbyRequest request)
      {
         if (request.NumberOfRounds < 1)
            return BadRequest(new { Message = "Number of rounds must be at least 1." });

         if (request.GamesList == null || request.GamesList.Count == 0)
            return BadRequest(new { Message = "Games list cannot be empty." });

         if (request.GamesList.Count != request.NumberOfRounds)
            return BadRequest(new { Message = "Games list length must match number of rounds." });

         if (request.MaxPlayers < 2)
            return BadRequest(new { Message = "Max players must be at least 2." });

         var lobbyCode = await _lobbyService.CreateLobbyWithSettings(
            request.NumberOfRounds,
            request.GamesList,
            request.MaxPlayers
         );

         return Ok(new
         {
            Code = lobbyCode,
            Message = $"Lobby {lobbyCode} created successfully",
            NumberOfRounds = request.NumberOfRounds,
            GamesList = request.GamesList,
            MaxPlayers = request.MaxPlayers
         });
      }
   }
}

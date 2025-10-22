using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Services;
using Api.Models;
using Api.GameLogic;

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
      public ActionResult CanJoinMatch(string code)
      {
         var name = User.Identity?.Name;
         var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
         if (name is null || idClaim is null || !Guid.TryParse(idClaim.Value, out var id))
            return Unauthorized();

         var error = _lobbyService.CanJoinLobby(code, id);
         if (error is null)
            return Ok(new { Message = "Can join match." });
         return BadRequest(new { Message = error });
      }

      [HttpPost("{code}/leave")]
      public async Task<ActionResult> LeaveMatch(string code)
      {
         var name = User.Identity?.Name;
         var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
         if (name is null || idClaim is null || !Guid.TryParse(idClaim.Value, out var id))
            return Unauthorized();

         var success = await _lobbyService.LeaveMatch(code, id);
         if (!success)
            return BadRequest(new { Message = "Unable to leave match or match does not exist." });

         return Ok(new { Message = $"Left match {code} successfully." });
      }

      [HttpPost("create")]
      public async Task<ActionResult> CreateLobbyWithSettings([FromBody] CreateLobbyDto request)
      {
         if (request.NumberOfRounds < 1 || request.NumberOfRounds > 5)
            return BadRequest(new { Message = "Number of rounds must be between 1 and 5." });

         if (request.NumberOfPlayers < 2 || request.NumberOfPlayers > 10)
            return BadRequest(new { Message = "Number of players must be between 2 and 10." });

         if (!request.RandomGames)
         {
            if (request.GamesList == null || request.GamesList.Count == 0)
               return BadRequest(new { Message = "Games list cannot be empty when not using random games." });

            foreach (var gameName in request.GamesList)
            {
               if (string.IsNullOrWhiteSpace(gameName))
               {
                  return BadRequest(new { Message = "Game name cannot be empty." });
               }

               if (!GameFactory.ValidGameTypes.Contains(gameName))
               {
                  return BadRequest(new { Message = $"Invalid game: {gameName}." });
               }
            }
         }

         var lobbyCode = await _lobbyService.CreateLobbyWithSettings(
            request.NumberOfPlayers,
            request.NumberOfRounds,
            request.RandomGames,
            request.GamesList
         );

         return Ok(new
         {
            Code = lobbyCode,
            Message = $"Lobby {lobbyCode} created successfully"
         });
      }
   }
}

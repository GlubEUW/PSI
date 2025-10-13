using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Models;
using Api.Hubs;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LobbyController() : ControllerBase
{
   [Authorize]
   [HttpGet("{code}")]
   public ActionResult<LobbyInfoDto> GetLobbyInfo(string code)
   {
      var name = User.Identity?.Name;
      var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
      if (name is null || idClaim is null || !Guid.TryParse(idClaim.Value, out var id))
         return Unauthorized();
      if (MatchHub.TryGetSession(code, out var session) && session != null)
      {
         if (session.Players.Count >= session.Players.Capacity)
         {
            Console.WriteLine($"{name} failed to join match {code} — match full.");
            return Ok(new LobbyInfoDto
            {
               IsLobbyFull = true,
               IsNameTakenInLobby = false
            });
         }

         foreach (var player in session.Players)
         {
            if (player == name) 
            {
               Console.WriteLine($"{name} failed to join match {code} — duplicate names.");
               return Ok(new LobbyInfoDto
               {
                  IsLobbyFull = false,
                  IsNameTakenInLobby = true
               });
            }
         }
      }

      return Ok(new LobbyInfoDto
      {
         IsLobbyFull = false,
         IsNameTakenInLobby = false
      });
   }

}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSI.Api.Models;
using PSI.Api.Services;

namespace PSI.Api.Controllers
{
   [Route("api/[controller]")]
   [ApiController]
   public class UserController(IAuthService authService) : ControllerBase
   {
      [HttpPut("guest")]
      public ActionResult<string> GuestCreate(GuestDto request)
      {
         var token = authService.GuestCreate(request);

         if (token is null)
            return BadRequest("Name is required.");

         return Ok(token);
      }

      [Authorize]
      [HttpGet("guest")]
      public ActionResult<GuestDto> GetGuestInfo()
      {
         var name = User.Identity?.Name;
         var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
         if (name is null || idClaim is null || !Guid.TryParse(idClaim.Value, out var id))
            return Unauthorized();

         var guest = new GuestDto
         {
            Name = name
         };

         return Ok(guest);
      }
   }
}

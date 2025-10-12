using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Models;
using Api.Services;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IAuthService authService) : ControllerBase
{
   [HttpPut("guest")]
   public ActionResult<string> GuestCreate(UserDto request)
   {
      var token = authService.GuestCreate(request);

      if (token is null)
         return BadRequest("Name is required.");

      return Ok(token);
   }

   [Authorize]
   [HttpGet("guest")]
   public ActionResult<UserDto> GetGuestInfo()
   {
      var name = User.Identity?.Name;
      var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
      if (name is null || idClaim is null || !Guid.TryParse(idClaim.Value, out var id))
         return Unauthorized();

      var user = new UserDto(name);

      return Ok(user);
   }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSI.Api.Entities;
using PSI.Api.Models;
using PSI.Api.Services;

namespace PSI.Api.Controllers
{
   [Route("api/[controller]")]
   [ApiController]
   public class AuthController(IAuthService authService) : ControllerBase
   {
      public static Guest guest = new();

      [HttpPost("guestLogin")]
      public ActionResult<string> GuestLogin(GuestDto request)
      {
         var token = authService.GuestLogin(request);

         if (token is null)
            return BadRequest("Name is required.");

         return Ok(token);
      }

      [Authorize]
      [HttpGet]
      public IActionResult AuthenticatedOnlyEndPoint()
      {
         return Ok("You are authenticated.");
      }
   }
}

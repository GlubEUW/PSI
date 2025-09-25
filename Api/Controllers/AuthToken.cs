using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PSI.Api.Entities;
using PSI.Api.Models;

namespace PSI.Api.Controllers
{
   [Route("api/[controller]")]
   [ApiController]
   public class AuthController : ControllerBase
   {
      public static Api.Entities.Guest guest = new();

      [HttpPost("guestLogin")]
      public ActionResult<Guest> GuestLogin(GuestDto request)
      {
         if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest("Username is required.");

         return Ok("success");
      }
   }
}

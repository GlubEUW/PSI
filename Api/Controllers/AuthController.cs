using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController() : ControllerBase
{
   [Authorize]
   [HttpGet]
   public IActionResult AuthenticatedOnlyEndPoint()
   {
      return Ok("You are authenticated.");
   }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSI.Api.Entities;
using PSI.Api.Models;
using PSI.Api.Services;

namespace PSI.Api.Controllers
{
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
}

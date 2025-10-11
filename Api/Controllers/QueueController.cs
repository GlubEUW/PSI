using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QueueController : ControllerBase
{
   [Authorize]
   [HttpPost]
   public IActionResult JoinQueue()
   {
      return Ok("Joined");
   }

}

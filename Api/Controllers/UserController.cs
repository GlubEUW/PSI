using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Models;
using Api.Services;
using Api.Entities;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IAuthService authService) : ControllerBase
{
   [HttpPost("guest")]
   public ActionResult<string> GuestCreate(UserDto request)
   {
      var token = authService.GuestCreate(request);

      if (token is null)
         return BadRequest("Name is required.");

      return Ok(token);
   }

   [HttpPost("login")]
   public async Task<ActionResult<string>> Login(UserDto request)
   {
      var token = await authService.LoginAsync(request);

      if (token is null)
         return BadRequest("Invalid name or password.");

      return Ok(token);
   }

   [HttpPost("register")]
   public async Task<ActionResult<User>> Register(UserDto request)
   {
      var user = await authService.RegisterAsync(request);

      if (user is null)
         return BadRequest("Name already exists.");

      return Ok(user);
   }

   [Authorize]
   [HttpGet("userInfo")]
   public ActionResult<UserDto> GetUserInfo()
   {
      var name = User.Identity?.Name;
      var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
      if (name is null || idClaim is null || !Guid.TryParse(idClaim.Value, out var id))
         return Unauthorized();

      var user = new UserDto(name, id);

      return Ok(user);
   }
}

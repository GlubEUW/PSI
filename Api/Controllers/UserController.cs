using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Models;
using Api.Services;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IAuthService authService, IUserService userService) : ControllerBase
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
   public async Task<ActionResult> Register(UserDto request)
   {
      var user = await authService.RegisterAsync(request);

      if (user is null)
         return BadRequest("Name already exists.");

      return Ok();
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

   [Authorize]
   [HttpGet("gameStats")]
   public async Task<ActionResult<GameStatsDto>> GetGameStats()
   {
      var name = User.Identity?.Name;
      var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
      var roleClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role);

      if (name is null
         || idClaim is null
         || !Guid.TryParse(idClaim.Value, out var id)
         || roleClaim?.Value is not "RegisteredUser")
      {
         return Unauthorized();
      }

      var user = await userService.GetUserByIdAsync(id);
      if (user is null)
         return NotFound();

      var statsDto = user.ToGameStatsDto();
      statsDto.Name = user.Name;
      return Ok(statsDto);
   }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using PSI.Api.Entities;
using PSI.Api.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PSI.Api.Controllers
{
   [Route("api/[controller]")]
   [ApiController]
   public class AuthController(IConfiguration configuration) : ControllerBase
   {
      public static Guest guest = new();

      [HttpPost("guestLogin")]
      public ActionResult<Guest> GuestLogin(GuestDto request)
      {
         if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

         var guest = new Guest {Name = request.Name};
         string token = CreateToken(guest);

         return Ok(new {Token = token, GuestName = guest.Name});
      }
      private string CreateToken(Guest guest)
      {
         var claims = new List<Claim>
         {
            new Claim(ClaimTypes.Name, guest.Name)
         };

         var privateKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!)
         );

         var creds = new SigningCredentials(privateKey, SecurityAlgorithms.HmacSha256);

         var tokenDescriptor = new JwtSecurityToken(
            issuer: configuration.GetValue<string>("AppSettings:Issuer"),
            audience: configuration.GetValue<string>("AppSettings:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("AppSettings:TokenExpiryMinutes")),
            signingCredentials: creds
         );

         return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
      }
   }
}

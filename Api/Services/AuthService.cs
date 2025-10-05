using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PSI.Api.Entities;
using PSI.Api.Models;

namespace PSI.Api.Services
{
   public class AuthService(IConfiguration configuration) : IAuthService
   {
      public string? GuestCreate(GuestDto request)
      {
         var guest = new Guest();
         if (string.IsNullOrWhiteSpace(request.Name))
            return null;

         guest.Name = request.Name;

         return CreateToken(guest);
      }

      private string CreateToken(Guest guest)
      {
         var claims = new List<Claim>
         {
            new Claim(ClaimTypes.Name, guest.Name),
            new Claim(ClaimTypes.NameIdentifier, guest.Id.ToString())
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

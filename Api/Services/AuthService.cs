using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Api.Entities;
using Api.Models;

namespace Api.Services;

public class AuthService(IConfiguration configuration) : IAuthService
{
   public string? GuestCreate(UserDto request)
   {
      var guest = new User();
      if (string.IsNullOrWhiteSpace(request.Name))
         return null;

      guest.Id = Guid.NewGuid();
      guest.Name = request.Name;
      guest.Role = UserRole.Guest;
      
      return CreateToken(guest);
   }

   private string CreateToken(User user)
   {
      var claims = new List<Claim>
         {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
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

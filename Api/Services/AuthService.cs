using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Api.Entities;
using Api.Models;
using Api.Enums;

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

      var handler = new JwtSecurityTokenHandler();
      var tokenString = handler.WriteToken(tokenDescriptor);

      var filePath = Path.Combine(AppContext.BaseDirectory, "tokens.txt");
      Console.WriteLine($"Storing token for user {user.Name} at {filePath}");
      using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
      using (var writer = new StreamWriter(stream, Encoding.UTF8))
      {
         writer.WriteLine(tokenString);
      }

      string? lastToken = null;
      using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      using (var reader = new StreamReader(stream, Encoding.UTF8))
      {
         string? line;
         while ((line = reader.ReadLine()) != null)
         {
            lastToken = line;
         }
      }

      return lastToken ?? string.Empty;
   }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Api.Entities;
using Api.Models;
using Api.Data;

namespace Api.Services;

public class AuthService(DatabaseContext context, IConfiguration configuration) : IAuthService
{
   public string? GuestCreate(UserDto request)
   {
      var guest = new Guest();
      if (string.IsNullOrWhiteSpace(request.Name))
         return null;

      guest.Id = Guid.NewGuid();
      guest.Name = request.Name;

      return CreateToken(guest);
   }

   public async Task<string?> LoginAsync(UserDto request)
   {
      var user = await context.Users.FirstOrDefaultAsync(u => u.Name == request.Name);

      if (user is null)
         return null;

      if (request.Password is null
         || new PasswordHasher<RegisteredUser>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
         == PasswordVerificationResult.Failed)
         return null;

      return CreateToken(user);
   }

   public async Task<RegisteredUser?> RegisterAsync(UserDto request)
   {
      if (await context.Users.AnyAsync(u => u.Name == request.Name))
         return null;

      var registeredUser = new RegisteredUser();

      string? hashedPassword = null;
      if (!string.IsNullOrWhiteSpace(request.Password))
      {
         hashedPassword = new PasswordHasher<RegisteredUser>()
            .HashPassword(registeredUser, request.Password);
      }

      registeredUser.Id = Guid.NewGuid();
      registeredUser.Name = request.Name;
      registeredUser.PasswordHash = hashedPassword ?? string.Empty;

      context.Users.Add(registeredUser);
      await context.SaveChangesAsync();

      return registeredUser;
   }

   private string CreateToken(User user)
   {
      var claims = new List<Claim>
      {
         new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
         new Claim(ClaimTypes.Name, user.Name)
      };

      if (user is RegisteredUser)
         claims.Add(new Claim(ClaimTypes.Role, "RegisteredUser"));
      else if (user is Guest)
         claims.Add(new Claim(ClaimTypes.Role, "Guest"));

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

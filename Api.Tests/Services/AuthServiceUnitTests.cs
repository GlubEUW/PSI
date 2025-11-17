using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using Api.Services;
using Api.Data;
using Api.Models;

namespace Api.Tests.Services;

public class AuthServiceUnitTests
{
   private static DatabaseContext CreateInMemoryContext(string dbName)
   {
      var options = new DbContextOptionsBuilder<DatabaseContext>()
         .UseInMemoryDatabase(databaseName: dbName)
         .Options;
      return new DatabaseContext(options);
   }

   private static IConfiguration CreateConfig()
   {
      var dict = new Dictionary<string, string?>
      {
         {"AppSettings:Token", "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"},
         {"AppSettings:Issuer", "TestIssuer"},
         {"AppSettings:Audience", "TestAudience"},
         {"AppSettings:TokenExpiryMinutes", "60"}
      };
      return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
   }

   [Fact]
   public void GuestCreate_ReturnsNull_WhenNameMissing()
   {
      using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
      var svc = new AuthService(ctx, CreateConfig());

      var result = svc.GuestCreate(new UserDto(string.Empty, Guid.Empty));

      Assert.Null(result);
   }

   [Fact]
   public void GuestCreate_ReturnsToken_WhenValidName()
   {
      using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
      var svc = new AuthService(ctx, CreateConfig());

      var result = svc.GuestCreate(new UserDto("guest", Guid.Empty));

      Assert.False(string.IsNullOrWhiteSpace(result));
   }

   [Fact]
   public async Task Login_ReturnsNull_WhenUserDoesNotExist()
   {
      using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
      var svc = new AuthService(ctx, CreateConfig());

      var result = await svc.LoginAsync(new UserDto("nobody", Guid.Empty) { Password = "pw" });

      Assert.Null(result);
   }

   [Fact]
   public async Task Login_ReturnsToken_WhenCredentialsValid()
   {
      using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
      var svc = new AuthService(ctx, CreateConfig());

      var name = "user1";
      var password = "P@ssw0rd!";
      await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = password });

      var result = await svc.LoginAsync(new UserDto(name, Guid.Empty) { Password = password });

      Assert.False(string.IsNullOrWhiteSpace(result));
   }

   [Fact]
   public async Task Login_ReturnsNull_WhenPasswordInvalid()
   {
      using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
      var svc = new AuthService(ctx, CreateConfig());

      var name = "user2";
      await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = "correct" });

      var result = await svc.LoginAsync(new UserDto(name, Guid.Empty) { Password = "wrong" });

      Assert.Null(result);
   }

   [Fact]
   public async Task Register_ReturnsNull_WhenNameExists()
   {
      using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
      var svc = new AuthService(ctx, CreateConfig());

      var name = "dup";
      await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = "x" });

      var result = await svc.RegisterAsync(new UserDto(name, Guid.Empty) { Password = "y" });

      Assert.Null(result);
   }

   [Fact]
   public async Task Register_ReturnsUser_WhenNew()
   {
      using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
      var svc = new AuthService(ctx, CreateConfig());

      var result = await svc.RegisterAsync(new UserDto("new", Guid.Empty) { Password = "pw" });

      Assert.NotNull(result);
      Assert.Equal("new", result!.Name);
      Assert.NotEqual(Guid.Empty, result.Id);
      Assert.False(string.IsNullOrWhiteSpace(result.PasswordHash));
   }
}

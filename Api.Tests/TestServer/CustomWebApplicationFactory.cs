using Api.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.TestServer;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
   protected override void ConfigureWebHost(IWebHostBuilder builder)
   {
      builder.UseEnvironment("Development");

      builder.ConfigureServices(services =>
      {

         var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DatabaseContext>));
         if (dbContextDescriptor != null)
         {
            services.Remove(dbContextDescriptor);
         }
         services.AddDbContext<DatabaseContext>(options => options.UseInMemoryDatabase($"Tests_{Guid.NewGuid()}"));


         services.AddAuthentication(options =>
           {
              options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
              options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
           })
           .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
      });
   }
}

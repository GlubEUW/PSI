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

         var dbContextOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DatabaseContext>));
         if (dbContextOptionsDescriptor is not null)
         {
            services.Remove(dbContextOptionsDescriptor);
         }

         var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DatabaseContext));
         if (dbContextDescriptor is not null)
         {
            services.Remove(dbContextDescriptor);
         }

         var dbContextFactoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<DatabaseContext>));
         if (dbContextFactoryDescriptor is not null)
         {
            services.Remove(dbContextFactoryDescriptor);
         }

         services.AddDbContextFactory<DatabaseContext>(options => options.UseInMemoryDatabase($"Tests_{Guid.NewGuid()}"));


         services.AddAuthentication(options =>
           {
              options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
              options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
           })
           .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
      });
   }
}

using LogisticsSystem.Api;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsSystem.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LogisticsSystem"] = "Server=(localdb)\\mssqllocaldb;Database=LogisticsSystem_Test;Trusted_Connection=True;MultipleActiveResultSets=true",
                    ["Jwt:SecretKey"] = "TestSuperSecretKeyForIntegrationTests1234567890!",
                    ["Jwt:Issuer"] = "LogisticsSystem",
                    ["Jwt:Audience"] = "LogisticsSystemUsers"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing ApplicationDbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        }
    }
}

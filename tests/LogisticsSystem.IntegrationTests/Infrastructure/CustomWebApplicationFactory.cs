using LogisticsSystem.Api;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.Infrastructure.Persistence.Interceptors;
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
                    ["Jwt:Audience"] = "LogisticsSystemUsers",
                    ["Jwt:AccessTokenExpirationMinutes"] = "60",
                    ["Jwt:RefreshTokenExpirationDays"] = "7"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing ApplicationDbContext and DbContextOptions registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType.Name.Contains("DbContextOptions")).ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    var interceptor = sp.GetService<AuditSaveChangesInterceptor>();
                    if (interceptor != null)
                    {
                        options.AddInterceptors(interceptor);
                    }
                });
            });
        }
    }
}

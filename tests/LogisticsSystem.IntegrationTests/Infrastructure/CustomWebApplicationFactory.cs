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
                    ["ConnectionStrings:LogisticsSystem"] = "Server=127.0.0.1,1433;Database=LogisticsSystem_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=True;Encrypt=False;",
                    ["Jwt:SecretKey"] = "TestSuperSecretKeyForIntegrationTests1234567890!",
                    ["Jwt:Issuer"] = "LogisticsSystem",
                    ["Jwt:Audience"] = "LogisticsSystemUsers",
                    ["Jwt:AccessTokenExpirationMinutes"] = "60",
                    ["Jwt:RefreshTokenExpirationDays"] = "7"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove all background IHostedService registrations (e.g. Hangfire background server) during testing
                var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                foreach (var hs in hostedServices)
                {
                    services.Remove(hs);
                }

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

                // Mock IShipmentAssignmentScheduler to avoid Hangfire dependency in tests
                var schedulerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(LogisticsSystem.Application.Common.Interfaces.Services.IShipmentAssignmentScheduler));
                if (schedulerDescriptor != null)
                {
                    services.Remove(schedulerDescriptor);
                }
                services.AddScoped<LogisticsSystem.Application.Common.Interfaces.Services.IShipmentAssignmentScheduler, FakeShipmentAssignmentScheduler>();
            });
        }
    }

    public class FakeShipmentAssignmentScheduler : LogisticsSystem.Application.Common.Interfaces.Services.IShipmentAssignmentScheduler
    {
        public void Schedule(Guid shipmentId)
        {
            // No-op for integration testing
        }
    }
}

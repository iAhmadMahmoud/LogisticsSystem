using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogisticsSystem.Infrastructure.Persistence.Health
{
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy.")
                    : HealthCheckResult.Unhealthy("Database connection failed.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed with an exception.", ex);
            }
        }
    }
}

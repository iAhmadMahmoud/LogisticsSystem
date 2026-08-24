using System.Diagnostics;
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
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["latencyMs"] = stopwatch.ElapsedMilliseconds,
                    ["provider"] = dbContext.Database.ProviderName ?? "Unknown"
                };

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy.", data)
                    : HealthCheckResult.Unhealthy("Database connection failed.", data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var data = new Dictionary<string, object>
                {
                    ["latencyMs"] = stopwatch.ElapsedMilliseconds
                };

                return HealthCheckResult.Unhealthy("Database connection could not be established.", ex, data);
            }
        }
    }
}

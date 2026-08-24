using System.Diagnostics;
using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogisticsSystem.Infrastructure.Persistence.Health
{
    public sealed class HangfireHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var storage = JobStorage.Current;
                if (storage == null)
                {
                    return Task.FromResult(HealthCheckResult.Degraded("Hangfire job storage is not initialized."));
                }

                var monitoringApi = storage.GetMonitoringApi();
                var servers = monitoringApi.Servers();

                stopwatch.Stop();
                var data = new Dictionary<string, object>
                {
                    ["serverCount"] = servers.Count,
                    ["latencyMs"] = stopwatch.ElapsedMilliseconds
                };

                return Task.FromResult(HealthCheckResult.Healthy("Hangfire background server and storage are operational.", data));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var data = new Dictionary<string, object>
                {
                    ["latencyMs"] = stopwatch.ElapsedMilliseconds
                };

                return Task.FromResult(HealthCheckResult.Degraded("Hangfire storage connection check failed.", ex, data));
            }
        }
    }
}

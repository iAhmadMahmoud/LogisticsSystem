using LogisticsSystem.Infrastructure.Authentication.Email;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LogisticsSystem.Infrastructure.Persistence.Health
{
    public sealed class EmailHealthCheck : IHealthCheck
    {
        private readonly EmailOptions _options;

        public EmailHealthCheck(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                ["provider"] = _options.Provider
            };

            if (string.Equals(_options.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_options.SmtpHost))
                {
                    return Task.FromResult(HealthCheckResult.Degraded("SMTP provider is selected but SmtpHost is empty.", data: data));
                }

                if (_options.SmtpPort <= 0 || _options.SmtpPort > 65535)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy($"Invalid SMTP port: {_options.SmtpPort}.", data: data));
                }
            }

            return Task.FromResult(HealthCheckResult.Healthy("Email provider configuration is valid.", data));
        }
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LogisticsSystem.Api.Common.Extensions
{
    public static class CorsPolicies
    {
        public const string Default = "DefaultCorsPolicy";
    }

    public static class CorsExtensions
    {
        public static IServiceCollection AddCustomCors(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var configuredOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            // Distinct non-empty origins
            var allowedOrigins = configuredOrigins
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Distinct()
                .ToArray();

            // In Development or Testing environments, ensure standard local client ports are available if none specified
            if (allowedOrigins.Length == 0 && (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
            {
                allowedOrigins = new[]
                {
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "http://localhost:4200",
                    "https://localhost:7001"
                };
            }

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicies.Default, policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins);
                    }

                    policy
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Retry-After", "Location", "Content-Disposition");
                });
            });

            return services;
        }
    }
}

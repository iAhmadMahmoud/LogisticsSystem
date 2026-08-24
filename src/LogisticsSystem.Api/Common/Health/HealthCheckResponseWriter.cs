using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogisticsSystem.Api.Common.Health
{
    public static class HealthCheckResponseWriter
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task WriteDetailedResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString(),
                timestamp = DateTime.UtcNow,
                entries = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration.ToString(),
                        data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
                        tags = entry.Value.Tags
                    })
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, response, SerializerOptions);
        }

        public static async Task WriteMinimalResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, response, SerializerOptions);
        }
    }
}

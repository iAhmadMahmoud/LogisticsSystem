using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsSystem.Api.Common.Extensions
{
    public static class RateLimiterPolicies
    {
        public const string Auth = "AuthLimiter";
        public const string Admin = "AdminLimiter";
        public const string Tracking = "TrackingLimiter";
    }

    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/problem+json";

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    }
                    else
                    {
                        context.HttpContext.Response.Headers.RetryAfter = "60";
                    }

                    var problemDetails = new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc6585#section-4",
                        Title = "Too Many Requests",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = "Rate limit exceeded. Please try again later.",
                        Instance = context.HttpContext.Request.Path
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                };

                // Global fallback limiter for all endpoints (100 req/min per IP/User)
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var partitionKey = GetUserOrIpKey(httpContext, "global");

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                // Authentication endpoints limiter: 5 requests/min per IP
                options.AddPolicy(RateLimiterPolicies.Auth, httpContext =>
                {
                    var partitionKey = GetClientIpKey(httpContext, "auth");

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                // Administrative endpoints limiter: 20 requests/min per User/IP
                options.AddPolicy(RateLimiterPolicies.Admin, httpContext =>
                {
                    var partitionKey = GetUserOrIpKey(httpContext, "admin");

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                // Tracking and telematics limiter: 120 requests/min per User/IP (supports high-frequency GPS ingestion)
                options.AddPolicy(RateLimiterPolicies.Tracking, httpContext =>
                {
                    var partitionKey = GetUserOrIpKey(httpContext, "tracking");

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });

            return services;
        }

        private static string GetClientIpKey(HttpContext httpContext, string prefix)
        {
            var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? httpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "127.0.0.1";

            return $"{prefix}:{clientIp}";
        }

        private static string GetUserOrIpKey(HttpContext httpContext, string prefix)
        {
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    return $"{prefix}:user:{userId}";
                }
            }

            return GetClientIpKey(httpContext, prefix);
        }
    }
}

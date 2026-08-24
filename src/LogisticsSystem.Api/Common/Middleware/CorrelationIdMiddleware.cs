using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace LogisticsSystem.Api.Common.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string CorrelationIdHeaderName = "X-Correlation-ID";
        public const string CorrelationIdItemKey = "CorrelationId";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrCreateCorrelationId(context);

            context.Items[CorrelationIdItemKey] = correlationId;

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
                }
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues headerValues) &&
                !string.IsNullOrWhiteSpace(headerValues.ToString()))
            {
                return headerValues.ToString();
            }

            if (context.Request.Headers.TryGetValue("X-Request-ID", out StringValues reqHeaderValues) &&
                !string.IsNullOrWhiteSpace(reqHeaderValues.ToString()))
            {
                return reqHeaderValues.ToString();
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}

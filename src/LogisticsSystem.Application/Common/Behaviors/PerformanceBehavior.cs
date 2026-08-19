using System.Diagnostics;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Application.Common.Behaviors
{
    public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const int ThresholdMilliseconds = 500;
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUserService;

        public PerformanceBehavior(
            ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();

            var response = await next();

            timer.Stop();

            var elapsedMilliseconds = timer.ElapsedMilliseconds;

            if (elapsedMilliseconds > ThresholdMilliseconds)
            {
                var requestName = typeof(TRequest).Name;
                var userId = GetCurrentUserIdSafely();

                _logger.LogWarning(
                    "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) for User: {UserId}",
                    requestName,
                    elapsedMilliseconds,
                    userId);
            }

            return response;
        }

        private string GetCurrentUserIdSafely()
        {
            try
            {
                var id = _currentUserService.UserId;
                return id == Guid.Empty ? "Anonymous" : id.ToString();
            }
            catch (UnauthorizedAccessException)
            {
                return "Anonymous";
            }
        }
    }
}

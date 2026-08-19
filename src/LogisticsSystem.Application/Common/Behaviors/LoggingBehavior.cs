using System.Diagnostics;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Application.Common.Behaviors
{
    public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUserService;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
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
            var requestName = typeof(TRequest).Name;
            var userId = GetCurrentUserIdSafely();

            _logger.LogInformation(
                "Handling {RequestName} for User: {UserId}",
                requestName,
                userId);

            var timer = Stopwatch.StartNew();

            var response = await next();

            timer.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName,
                timer.ElapsedMilliseconds);

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

using LogisticsSystem.Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.SignalR
{
    public sealed class NotificationRealtimeService : INotificationRealtimeService
    {
        private readonly  IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationRealtimeService> _logger;

        public NotificationRealtimeService(IHubContext<NotificationHub> hubContext, ILogger<NotificationRealtimeService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }



        public async Task SendAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending SignalR notification to User {UserId}. Title: {Title}", userId, title);

            await _hubContext
              .Clients
              .User(userId.ToString())
              .SendAsync("NotificationReceived",
                  new
                  {
                      title,
                      message
                  },
                  cancellationToken);

            _logger.LogInformation("SignalR notification sent successfully to User {UserId}", userId);

        }
    }
}

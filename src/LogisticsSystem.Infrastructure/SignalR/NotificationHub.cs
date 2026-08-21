using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.SignalR
{
    [Authorize]
    public sealed class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Notification SignalR client connected: {UserId}", Context.UserIdentifier);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Notification SignalR client disconnected: {UserId}", Context.UserIdentifier);
            return base.OnDisconnectedAsync(exception);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LogisticsSystem.Infrastructure.SignalR
{
    [Authorize]
    public sealed class NotificationHub : Hub
    {

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"SignalR connected: {Context.UserIdentifier}");

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"SignalR disconnected: {Context.UserIdentifier}");

            return base.OnDisconnectedAsync(exception);
        }
    }
}

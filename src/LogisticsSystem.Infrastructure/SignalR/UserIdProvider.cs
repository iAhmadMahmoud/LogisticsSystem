using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LogisticsSystem.Infrastructure.SignalR
{
    public sealed class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?
                .FindFirstValue(ClaimTypes.NameIdentifier)
                ?? connection.User?
                    .FindFirstValue("sub");
        }
    }
}

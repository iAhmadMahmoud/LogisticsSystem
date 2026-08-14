using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface INotificationService
    {
        Task CreateAsync(Guid userId, string title, string message, NotificationType type, CancellationToken cancellationToken = default);
        Task<PagedResult<NotificationResponse>> GetMyNotificationAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
        Task SendRealtimeAsync(Guid userId,string title,string message,CancellationToken cancellationToken = default);

    }
}

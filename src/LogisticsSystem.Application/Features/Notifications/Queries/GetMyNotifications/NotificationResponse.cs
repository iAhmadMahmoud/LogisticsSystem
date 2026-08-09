using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications
{
    public sealed record NotificationResponse(Guid Id, string Title, string Message, NotificationType Type, bool IsRead, DateTime? ReadAt, DateTime CreatedAt);
}

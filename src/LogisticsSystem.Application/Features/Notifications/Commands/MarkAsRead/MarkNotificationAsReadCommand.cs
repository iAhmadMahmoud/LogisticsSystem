using MediatR;

namespace LogisticsSystem.Application.Features.Notifications.Commands.MarkAsRead
{

    public sealed record MarkNotificationAsReadCommand(Guid notificationId) : IRequest;
}

using LogisticsSystem.Application.Common.Models;
using MediatR;

namespace LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications
{
    public sealed record GetMyNotificationsQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<NotificationResponse>>;
}

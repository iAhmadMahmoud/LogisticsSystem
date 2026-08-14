using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Models;
using MediatR;

namespace LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications
{
    public sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, PagedResult<NotificationResponse>>
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        public GetMyNotificationsQueryHandler(INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResult<NotificationResponse>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            return await _notificationService.GetMyNotificationAsync(
                _currentUserService.UserId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);
        }
    }
}

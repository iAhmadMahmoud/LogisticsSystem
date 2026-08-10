using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Notifications.Commands.MarkAsRead
{
    public sealed class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand>
    {


        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public MarkNotificationAsReadCommandHandler(INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            await _notificationService.MarkAsReadAsync(
                request.notificationId,
                _currentUserService.UserId,
                cancellationToken);
        }
    }
}

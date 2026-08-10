using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using LogisticsSystem.Application.Features.Notifications.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class NotificationService : INotificationService
    {
        private readonly IGenericRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        public NotificationService(IGenericRepository<Notification> notificationRepository, IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task CreateAsync(Guid userId, string title, string message, NotificationType type, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type
            };

            await _notificationRepository.AddAsync(notification,cancellationToken);


        }

        public async Task<PagedResult<NotificationResponse>> GetMyNotificationAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var specification = new MyNotificationsSpecification(userId, pageNumber, pageSize);

            var totalCount = await _notificationRepository.CountAsync(specification, cancellationToken);
            var notifications = await _notificationRepository.ListAsync(specification, cancellationToken);

            var items = notifications
                .Select(notification=>new NotificationResponse(
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    notification.Type,
                    notification.IsRead,
                    notification.ReadAt,
                    notification.CreatedAt))
                .ToList();

            return new PagedResult<NotificationResponse>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };


        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
        {
            var specification = new NotificationByIdSpecification(
                    notificationId,
                    userId);

            var notification = await _notificationRepository
                .FirstOrDefaultAsync(
                    specification,
                    cancellationToken);

            if (notification is null)
            {
                throw new KeyNotFoundException("Notification not found.");
            }

            if (notification.IsRead)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            _notificationRepository.Update(notification);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Notifications.Specifications
{
    public sealed class NotificationByIdSpecification
        : BaseSpecification<Notification>
    {
        public NotificationByIdSpecification(
            Guid notificationId,
            Guid userId)
            : base(x =>
                x.Id == notificationId &&
                x.UserId == userId)
        {
        }
    }
}

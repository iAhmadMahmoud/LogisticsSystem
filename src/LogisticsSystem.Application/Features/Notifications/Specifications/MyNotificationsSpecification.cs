using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Notifications.Specifications
{
    public sealed class MyNotificationsSpecification : BaseSpecification<Notification>
    {
        public MyNotificationsSpecification(Guid userId,int pageNumber,int pageSize) : base(x=>x.UserId == userId)
        {
            ApplyOrderByDescending(x => x.CreatedAt);

            ApplyPaging((pageNumber -1)*pageSize, pageSize);
        }
    }
}

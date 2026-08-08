using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Dispatch.Specifications
{
    public sealed class MyAssignmentsSpecification : BaseSpecification<DispatchAssignment>
    {
        public MyAssignmentsSpecification(
            Guid driverId,
            AssignmentStatus? status,
            int pageNumber,
            int pageSize):base(x=>x.DriverId == driverId &&(status == null || x.Status == status))
        {
            ApplyOrderByDescending(x=>x.SentAt);

            var skip = (pageNumber - 1) * pageSize;

            ApplyPaging(skip, pageSize);
        }
    }
}

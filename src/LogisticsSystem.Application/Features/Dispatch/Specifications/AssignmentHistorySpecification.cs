using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Dispatch.Specifications
{
    public sealed class AssignmentHistorySpecification : BaseSpecification<DispatchAssignment>
    {
        public AssignmentHistorySpecification(
            Guid shipmentId,
            int pageNumber,
            int pageSize)
            : base(x => x.ShipmentId == shipmentId)
        {
            ApplyOrderByDescending(x => x.AttemptNumber);

            ApplyPaging(
                (pageNumber - 1) * pageSize,
                pageSize);
        }
    }
}

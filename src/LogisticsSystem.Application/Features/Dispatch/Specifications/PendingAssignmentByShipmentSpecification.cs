using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Dispatch.Specifications
{
    public sealed class PendingAssignmentByShipmentSpecification : BaseSpecification<DispatchAssignment>
    {
        public PendingAssignmentByShipmentSpecification(Guid shipmentId)
            : base(x => x.ShipmentId == shipmentId && x.Status == AssignmentStatus.Pending)
        {
        }
    }
}

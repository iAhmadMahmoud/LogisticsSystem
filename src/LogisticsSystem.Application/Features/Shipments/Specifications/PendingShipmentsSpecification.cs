using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Shipments.Specifications
{
    public sealed class PendingShipmentsSpecification :BaseSpecification<Shipment>
    {
        public PendingShipmentsSpecification():base(s=>s.Status == ShipmentStatus.Pending && s.DriverId == null && !s.DispatchAssignments.Any(d=>d.Status == AssignmentStatus.Pending))
        {
            ApplyOrderBy(s => s.ScheduledAt);
        }
    }
}

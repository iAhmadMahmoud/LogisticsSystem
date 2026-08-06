using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Shipments.Specifications
{
    public sealed class PendingShipmentsSpecification :BaseSpecification<Shipment>
    {
        public PendingShipmentsSpecification():base(s=>s.Status == Domain.Enums.ShipmentStatus.Pending && s.DriverId == null)
        {
            ApplyOrderBy(s => s.ScheduledAt);
        }
    }
}

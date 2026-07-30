using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Specifications
{
    public sealed class ShipmentTrackingByShipmentIdSpecification : BaseSpecification<ShipmentTracking>
    {
        public ShipmentTrackingByShipmentIdSpecification(Guid shipmentId) : base(x=>x.ShipmentId ==shipmentId)
        {
            ApplyOrderByDescending(x => x.RecordedAt);
        }
    }
}

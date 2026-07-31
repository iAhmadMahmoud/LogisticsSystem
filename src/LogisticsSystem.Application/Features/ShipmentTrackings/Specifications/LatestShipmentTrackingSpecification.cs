using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Specifications
{
    public sealed class LatestShipmentTrackingSpecification : BaseSpecification<ShipmentTracking>
    {
        public LatestShipmentTrackingSpecification(Guid shipmentId)
        {
            AddCriteria(x => x.ShipmentId == shipmentId);

            ApplyOrderByDescending(x => x.RecordedAt);
        }
    }
}

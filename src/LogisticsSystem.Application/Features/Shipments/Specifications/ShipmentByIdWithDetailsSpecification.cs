using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Shipments.Specifications
{
    public sealed class ShipmentByIdWithDetailsSpecification : BaseSpecification<Shipment>
    {
        public ShipmentByIdWithDetailsSpecification(Guid shipmentId) : base(s => s.Id == shipmentId)
        {
            AddInclude(s => s.Driver!);
            AddInclude(s => s.ShipmentTrackings);
        }
    }
}

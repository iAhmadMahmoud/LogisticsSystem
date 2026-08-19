using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Shipments.Specifications
{
    public sealed class ShipmentByIdWithCustomerSpecification : BaseSpecification<Shipment>
    {
        public ShipmentByIdWithCustomerSpecification(Guid shipmentId) : base(s => s.Id == shipmentId)
        {
            AddInclude(s => s.Customer);
        }
    }
}

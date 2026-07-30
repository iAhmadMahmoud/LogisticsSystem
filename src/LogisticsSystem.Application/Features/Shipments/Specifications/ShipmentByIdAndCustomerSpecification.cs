using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Shipments.Specifications
{
    public sealed class ShipmentByIdAndCustomerSpecification : BaseSpecification<Shipment>
    {
        public ShipmentByIdAndCustomerSpecification(Guid shipmentId, Guid customerId) : base(s => s.Id == shipmentId && s.CustomerId == customerId)
        { }
    }
}

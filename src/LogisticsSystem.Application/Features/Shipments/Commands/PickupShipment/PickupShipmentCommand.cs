using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.PickupShipment
{
    public sealed record PickupShipmentCommand(Guid ShipmentId) : IRequest;
}

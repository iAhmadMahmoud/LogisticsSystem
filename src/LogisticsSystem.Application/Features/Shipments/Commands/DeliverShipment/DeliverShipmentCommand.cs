using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeliverShipment
{
    public sealed record DeliverShipmentCommand(Guid ShipmentId) : IRequest;
}

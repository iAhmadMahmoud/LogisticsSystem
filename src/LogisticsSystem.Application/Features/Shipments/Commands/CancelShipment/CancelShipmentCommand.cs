using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment
{
    public sealed record CancelShipmentCommand(Guid ShipmentId) : IRequest;

}

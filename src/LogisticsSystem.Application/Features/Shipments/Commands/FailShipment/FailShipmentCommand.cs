using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.FailShipment
{
    public sealed record FailShipmentCommand(Guid ShipmentId) : IRequest;
}

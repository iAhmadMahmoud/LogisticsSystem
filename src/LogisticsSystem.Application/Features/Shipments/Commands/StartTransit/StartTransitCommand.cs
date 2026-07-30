using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.StartTransit
{
    public sealed record StartTransitCommand(Guid ShipmentId) : IRequest;
}

using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.AssignDriver
{
    public sealed record AssignDriverCommand(Guid ShipmentId, Guid DriverId) : IRequest;
}

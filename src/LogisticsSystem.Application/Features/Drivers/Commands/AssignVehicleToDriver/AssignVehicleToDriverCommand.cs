using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.AssignVehicleToDriver
{
    public sealed record AssignVehicleToDriverCommand(
        Guid DriverId,
        Guid VehicleId) : IRequest;
}

using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.RemoveVehicleFromDriver
{
    public sealed record RemoveVehicleFromDriverCommand(Guid DriverId) : IRequest;
}

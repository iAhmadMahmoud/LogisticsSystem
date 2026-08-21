using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Commands.DeleteVehicle
{
    public sealed record DeleteVehicleCommand(Guid Id) : IRequest;
}

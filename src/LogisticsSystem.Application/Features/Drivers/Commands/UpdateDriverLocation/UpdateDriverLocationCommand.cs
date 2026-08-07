using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverLocation
{
    public sealed record UpdateDriverLocationCommand(double Latitude, double Longitude) : IRequest;
}

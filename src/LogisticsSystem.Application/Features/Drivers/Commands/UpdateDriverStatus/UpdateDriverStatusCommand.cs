using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverStatus
{
    public sealed record UpdateDriverStatusCommand(DriverStatus Status) : IRequest;
    
    
}

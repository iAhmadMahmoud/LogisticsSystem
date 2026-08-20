using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Commands.UpdateVehicle
{
    public sealed record UpdateVehicleCommand(
        Guid Id,
        string PlateNumber,
        string Brand,
        string Model,
        int ManufacturingYear,
        string Color,
        VehicleType Type,
        decimal Capacity,
        bool IsActive) : IRequest<VehicleDto>;
}

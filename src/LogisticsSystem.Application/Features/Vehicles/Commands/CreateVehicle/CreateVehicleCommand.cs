using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Commands.CreateVehicle
{
    public sealed record CreateVehicleCommand(
        string PlateNumber,
        string Brand,
        string Model,
        int ManufacturingYear,
        string Color,
        VehicleType Type,
        decimal Capacity) : IRequest<VehicleDto>;
}

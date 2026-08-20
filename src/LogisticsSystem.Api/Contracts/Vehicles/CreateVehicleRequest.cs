using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Api.Contracts.Vehicles
{
    public sealed record CreateVehicleRequest(
        string PlateNumber,
        string Brand,
        string Model,
        int ManufacturingYear,
        string Color,
        VehicleType Type,
        decimal Capacity);
}

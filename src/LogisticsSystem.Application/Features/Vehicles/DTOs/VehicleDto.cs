using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Vehicles.DTOs
{
    public sealed class VehicleDto
    {
        public Guid Id { get; init; }
        public string PlateNumber { get; init; } = string.Empty;
        public string Brand { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public int ManufacturingYear { get; init; }
        public string Color { get; init; } = string.Empty;
        public VehicleType Type { get; init; }
        public decimal Capacity { get; init; }
        public bool IsActive { get; init; }
        public Guid? DriverId { get; init; }
        public bool IsAssigned => DriverId.HasValue;
        public DateTime CreatedAt { get; init; }
    }
}

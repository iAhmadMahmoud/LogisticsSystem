using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetDriverById
{
    public sealed class DriverDetailsResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string LicenseNumber { get; init; } = string.Empty;
        public DriverStatus Status { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public Guid? VehicleId { get; init; }
    }
}

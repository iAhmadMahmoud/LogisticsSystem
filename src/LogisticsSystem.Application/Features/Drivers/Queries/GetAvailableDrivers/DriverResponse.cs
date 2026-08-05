namespace LogisticsSystem.Application.Features.Drivers.Queries.GetAvailableDrivers
{
    public sealed class DriverResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public double? latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

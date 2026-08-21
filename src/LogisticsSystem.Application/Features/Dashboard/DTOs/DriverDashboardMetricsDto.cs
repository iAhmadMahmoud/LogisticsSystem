namespace LogisticsSystem.Application.Features.Dashboard.DTOs
{
    public sealed class DriverDashboardMetricsDto
    {
        public int TotalDrivers { get; init; }
        public int AvailableDrivers { get; init; }
        public int BusyDrivers { get; init; }
        public int OfflineDrivers { get; init; }
        public int OnBreakDrivers { get; init; }
        public int SuspendedDrivers { get; init; }
        public int DriversWithVehicles { get; init; }
        public int DriversWithoutVehicles { get; init; }
        public int ActiveDrivers { get; init; }
        public int InactiveDrivers { get; init; }
    }
}

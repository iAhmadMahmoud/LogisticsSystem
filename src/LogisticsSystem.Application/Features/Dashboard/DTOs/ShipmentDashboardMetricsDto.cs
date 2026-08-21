namespace LogisticsSystem.Application.Features.Dashboard.DTOs
{
    public sealed class ShipmentDashboardMetricsDto
    {
        public int TotalShipments { get; init; }
        public int PendingShipments { get; init; }
        public int AssignedShipments { get; init; }
        public int PickedUpShipments { get; init; }
        public int InTransitShipments { get; init; }
        public int DeliveredShipments { get; init; }
        public int CancelledShipments { get; init; }
        public int FailedShipments { get; init; }
        public int ShipmentsCreatedToday { get; init; }
        public int ShipmentsDeliveredToday { get; init; }
    }
}

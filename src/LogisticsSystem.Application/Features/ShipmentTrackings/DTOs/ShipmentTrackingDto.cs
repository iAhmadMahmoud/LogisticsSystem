namespace LogisticsSystem.Application.Features.ShipmentTrackings.DTOs
{
    public sealed class ShipmentTrackingDto
    {
        public Guid Id { get; set; }
        public Guid ShipmentId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}

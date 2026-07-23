using LogisticsSystem.Domain.Common;

namespace LogisticsSystem.Domain.Entities
{
    public class ShipmentTracking :  BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime RecordedAt { get; set; }
        public Shipment Shipment { get; set; } = default!;
    }
}

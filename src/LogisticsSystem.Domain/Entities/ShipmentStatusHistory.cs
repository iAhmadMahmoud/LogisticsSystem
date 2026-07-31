using LogisticsSystem.Domain.Common;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Domain.Entities
{
    public class ShipmentStatusHistory : BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public ShipmentStatus Status { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public Guid? ChangedByUserId { get; set; }
        public Shipment Shipment { get; set; } = default!;
    }
}

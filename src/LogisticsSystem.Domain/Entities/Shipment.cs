using LogisticsSystem.Domain.Common;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Domain.Entities
{
    public class Shipment :  AuditableEntity
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Guid? DriverId { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public double DeliveryLatitude { get; set; }
        public double DeliveryLongitude { get; set; }
        public decimal Weight { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal ShippingCost { get; set; }

        public ShipmentPriority Priority { get; set; }
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
        public string? Notes { get; set; }
        public DateTime ScheduledAt { get; set; }

        public DateTime? AssignedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public Customer Customer { get; set; } = default!;
        public Driver? Driver { get; set; }
        public ICollection<ShipmentTracking> ShipmentTrackings { get; set; } = new List<ShipmentTracking>();
        public ICollection<DispatchAssignment> DispatchAssignments { get; set; } = new List<DispatchAssignment>();
        public ICollection<ShipmentStatusHistory> StatusHistory { get; set; } = new List<ShipmentStatusHistory>();

    }
}

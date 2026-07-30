using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Shipments.DTOs
{
    public sealed class ShipmentDto
    {
        public Guid Id { get; set; }
        public string TrackingNumber { get; init; } = default!;
        public string PickupAddress { get; init; } = default!;
        public string DeliveryAddress { get; init; } = default!;
        public decimal Weight { get; init; }
        public decimal ShippingCost { get; init; }
        public ShipmentPriority Priority { get; init; }
        public ShipmentStatus Status { get; init; }
        public Guid? DriverId { get; set; }

        public DateTime? AssignedAt { get; set; }
        public DateTime ScheduledAt { get; init; }


    }
}

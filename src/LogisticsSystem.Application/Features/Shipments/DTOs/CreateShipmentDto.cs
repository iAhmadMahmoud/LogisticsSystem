using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Shipments.DTOs
{
    public class CreateShipmentDto
    {
        public Guid CustomerId { get; set; }

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

        public string? Notes { get; set; }

        public DateTime ScheduledAt { get; set; }
    }
}

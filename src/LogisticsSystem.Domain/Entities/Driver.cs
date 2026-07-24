using LogisticsSystem.Domain.Common;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Domain.Entities
{
    public class Driver : AuditableEntity
    {
        public Guid UserId { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public DriverStatus Status { get; set; } = DriverStatus.Offline;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public ICollection<DispatchAssignment> DispatchAssignments { get; set; } = new List<DispatchAssignment>();

    }
}

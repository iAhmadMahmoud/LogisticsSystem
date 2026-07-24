using LogisticsSystem.Domain.Common;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Domain.Entities
{
    public class DispatchAssignment : AuditableEntity
    {
        public Guid ShipmentId { get; set; }
        public Guid DriverId { get; set; }
        public int AttemptNumber { get; set; }
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
        public DateTime SentAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public Shipment Shipment { get; set; } = default!;
        public Driver Driver { get; set; } = default!;
    }
}
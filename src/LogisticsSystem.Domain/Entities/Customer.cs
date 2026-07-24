using LogisticsSystem.Domain.Common;

namespace LogisticsSystem.Domain.Entities
{
    public class Customer : AuditableEntity
    {
        public Guid UserId { get; set; }
        public string? DefaultAddress { get; set; }

        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}

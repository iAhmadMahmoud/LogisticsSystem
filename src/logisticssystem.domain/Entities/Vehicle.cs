using LogisticsSystem.Domain.Common;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Domain.Entities
{
    public class Vehicle : AuditableEntity
    {
        public string PlateNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int ManufacturingYear { get; set; }
        public string Color {  get; set; } = string.Empty;
        public VehicleType Type { get; set; }
        public decimal Capacity { get; set; }
        public bool IsActive { get; set; } = true;
        public Driver? Driver { get; set; }
    }
}

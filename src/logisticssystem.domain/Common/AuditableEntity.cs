using LogisticsSystem.Domain.Common;

namespace LogisticsSystem.Domain.Common
{
    public abstract class AuditableEntity  : BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}

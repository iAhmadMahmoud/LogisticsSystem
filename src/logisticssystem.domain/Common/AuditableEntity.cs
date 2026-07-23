using LogisticsSystem.Domain.Common;

namespace LogisticsSystem.Domain.Common
{
    public abstract class AuditableEntity 
    {
        public DateTime CreatedOnUtc { get; protected set; }
        public DateTime? ModifiedOnUtc { get; protected set; }
        public Guid? CreatedBy { get; protected set; }
        public Guid? ModifiedBy { get; protected set; }
    }
}

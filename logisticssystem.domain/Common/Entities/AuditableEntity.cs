namespace LogisticsSystem.Domain.Common.Entities
{
    public abstract class AuditableEntity : AggregateRoot
    {
        public DateTime CreatedOnUtc { get; protected set; }
        public DateTime? ModifiedOnUtc { get; protected set; }
        public Guid? CreatedBy { get; protected set; }
        public Guid? ModifiedBy { get; protected set; }
    }
}

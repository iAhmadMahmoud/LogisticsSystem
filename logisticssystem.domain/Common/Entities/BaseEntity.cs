using LogisticsSystem.Domain.Common.Events;

namespace LogisticsSystem.Domain.Common.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }

        private readonly List<IDomainEvent> _domainEvents = new List<IDomainEvent>();

        public IReadOnlyCollection<IDomainEvent> domainEvents => _domainEvents.AsReadOnly();
        protected BaseEntity()
        {
            Id = Guid.CreateVersion7();
        }

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
        
    }
}

namespace LogisticsSystem.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get;  set; } = Guid.CreateVersion7();
}
namespace LogisticsSystem.Application.Features.Dashboard.DTOs
{
    public sealed class RecentActivityDto
    {
        public Guid Id { get; init; }
        public string ActivityType { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public Guid EntityId { get; init; }
        public string EntityType { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
        public Guid? UserId { get; init; }
    }
}

namespace LogisticsSystem.Application.Features.RoleManagement.DTOs
{
    public sealed class RoleDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int UserCount { get; init; }
        public bool IsSystemRole { get; init; }
    }
}

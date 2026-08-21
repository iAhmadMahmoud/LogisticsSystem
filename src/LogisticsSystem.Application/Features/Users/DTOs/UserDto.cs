namespace LogisticsSystem.Application.Features.Users.DTOs
{
    public sealed class UserDto
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public string? ProfileImageUrl { get; init; }
        public bool IsActive { get; init; }
        public IReadOnlyList<string> Roles { get; init; } = [];
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
    }
}

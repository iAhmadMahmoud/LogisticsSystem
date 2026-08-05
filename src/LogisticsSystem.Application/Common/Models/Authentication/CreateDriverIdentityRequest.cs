namespace LogisticsSystem.Application.Common.Models.Authentication
{
    public sealed class CreateDriverIdentityRequest
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;

    }
}

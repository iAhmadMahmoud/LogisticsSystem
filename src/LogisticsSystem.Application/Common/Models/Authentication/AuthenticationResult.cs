namespace LogisticsSystem.Application.Common.Models.Authentication
{
    public sealed class AuthenticationResult
    {
        public string AccessToken { get; init; } = string.Empty;

        public string RefreshToken { get; init; } = string.Empty;

        public DateTime ExpiresAt { get; init; }

        public bool EmailConfirmed { get; init; }

        public string UserName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;
    }
}
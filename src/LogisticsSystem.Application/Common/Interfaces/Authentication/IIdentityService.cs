using LogisticsSystem.Application.Common.Models.Authentication;

namespace LogisticsSystem.Application.Common.Interfaces.Authentication
{
    public interface IIdentityService
    {
        Task<AuthenticationResult> RegisterAsync(RegisterRequest request);
        Task<AuthenticationResult> LoginAsync (LoginRequest request);
        Task LogoutAsync(string refreshToken);
        Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
        Task ConfirmEmailAsync(string userId, string token);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task ChangePasswordAsync(ChangePasswordRequest request);
        Task<Guid> CreateDriverAsync(CreateDriverIdentityRequest request,CancellationToken cancellationToken = default);
        Task UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default);
    }
}

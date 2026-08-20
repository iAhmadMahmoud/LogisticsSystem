using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using LogisticsSystem.Application.Features.Users.DTOs;

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
        Task<UserInfoDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role, bool? isActive, string? searchTerm, CancellationToken cancellationToken = default);
        Task<UserDetailsDto?> GetUserDetailsByIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserDetailsDto> UpdateUserByAdminAsync(Guid userId, string firstName, string lastName, string? phoneNumber, string email, string userName, CancellationToken cancellationToken = default);
        Task SetUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
        Task DeactivateOrDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
        Task<RoleDto> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default);
        Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
        Task AssignRoleToUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
        Task RemoveRoleFromUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    }
}

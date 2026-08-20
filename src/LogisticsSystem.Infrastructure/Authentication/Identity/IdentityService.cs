using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Application.Specifications;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Authentication.Email;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using LogisticsSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace LogisticsSystem.Infrastructure.Authentication.Identity
{
    public sealed class IdentityService : IIdentityService
    {
        private static class ErrorMessages
        {
            public const string UserNotFound = "User not found.";
            public const string InvalidEmailOrPassword = "Invalid email or password.";
            public const string AccountDeactivated = "Your account has been deactivated.";
            public const string EmailNotConfirmed = "Please confirm your email before logging in.";
            public const string InvalidRefreshToken = "Invalid refresh token.";
            public const string RefreshTokenNoLongerValid = "Refresh token is no longer valid.";
            public const string UserAccountInactive = "User account is inactive.";
            public const string EmailAlreadyExists = "Email already exists.";
            public const string UsernameAlreadyExists = "Username already exists.";
        }

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IGenericRepository<RefreshToken> _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtOptions _jwtOptions;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IEmailSender _emailSender;
        private readonly EmailOptions _emailOptions;
        private readonly ICurrentUserService _currentUserService;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenGenerator jwtTokenGenerator,
            IUnitOfWork unitOfWork,
            IOptions<JwtOptions> jwtOptions,
            IGenericRepository<Customer> customerRepository,
            IRefreshTokenGenerator refreshTokenGenerator,
            IGenericRepository<RefreshToken> refreshTokenRepository,
            IEmailSender emailSender,
            IOptions<EmailOptions> emailOptions,
            ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _unitOfWork = unitOfWork;
            _jwtOptions = jwtOptions.Value;
            _customerRepository = customerRepository;
            _refreshTokenGenerator = refreshTokenGenerator;
            _refreshTokenRepository = refreshTokenRepository;
            _emailSender = emailSender;
            _emailOptions = emailOptions.Value;
            _currentUserService = currentUserService;
        }

        public async Task ChangePasswordAsync(ChangePasswordRequest request)
        {
            var userId = _currentUserService.UserId;

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            EnsureSucceeded(result);

            var specification = new ActiveRefreshTokensByUserSpecification(user.Id);

            var refreshTokens = await _refreshTokenRepository.ListAsync(specification);

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ConfirmEmailAsync(string userId, string token)
        {
            var user = await GetUserByIdOrThrowAsync(userId, ErrorMessages.UserNotFound);

            var decodedToken = DecodeToken(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            EnsureSucceeded(result);
        }
        public async Task<Guid> CreateDriverAsync(CreateDriverIdentityRequest request, CancellationToken cancellationToken = default)
        {
            var existignUser = await _userManager.FindByEmailAsync(request.Email);
            if (existignUser is not null)
            {
                throw new InvalidOperationException(ErrorMessages.EmailAlreadyExists);
            }

            var existingUsername = await _userManager.FindByNameAsync(request.UserName);

            if(existingUsername is not null)
            {
                throw new InvalidOperationException(ErrorMessages.UsernameAlreadyExists);
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = false,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user,request.Password);

            EnsureSucceeded(createResult);

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Driver);

            EnsureSucceeded(roleResult);

            return user.Id;
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null || !user.EmailConfirmed)
                return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetUrl = BuildCallbackUrl(_emailOptions.ResetPasswordUrl, user.Id, token);

            await SendResetPasswordEmailAsync(user.Email!, resetUrl);
        }

        public async Task<AuthenticationResult> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null)
            {
                throw new UnauthorizedAccessException(ErrorMessages.InvalidEmailOrPassword);
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(ErrorMessages.AccountDeactivated);
            }

            if (!user.EmailConfirmed)
            {
                throw new UnauthorizedAccessException(ErrorMessages.EmailNotConfirmed);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
            {
                throw new UnauthorizedAccessException(ErrorMessages.InvalidEmailOrPassword);
            }

            user.LastLoginAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            EnsureSucceeded(updateResult);

            return await CreateAuthenticationResultAsync(user);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var storedToken = await GetStoredRefreshTokenOrThrowAsync(refreshToken, ErrorMessages.InvalidRefreshToken);

            if (storedToken.IsRevoked)
                return;

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            _refreshTokenRepository.Update(storedToken);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await GetStoredRefreshTokenOrThrowAsync(refreshToken, ErrorMessages.InvalidRefreshToken);

            if (!storedToken.IsActive)
            {
                throw new UnauthorizedAccessException(ErrorMessages.RefreshTokenNoLongerValid);
            }

            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());

            if (user is null)
            {
                throw new UnauthorizedAccessException(ErrorMessages.UserNotFound);
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(ErrorMessages.UserAccountInactive);
            }

            var newRefreshToken = _refreshTokenGenerator.Generate(user.Id, _jwtOptions.RefreshTokenExpirationDays);

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshToken.Token;

            _refreshTokenRepository.Update(storedToken);

            await _refreshTokenRepository.AddAsync(newRefreshToken);

            await _unitOfWork.SaveChangesAsync();

            return await CreateAuthenticationResultAsync(user, newRefreshToken);
        }

        public async Task<AuthenticationResult> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);

            if (existingUser is not null)
            {
                throw new InvalidOperationException(ErrorMessages.EmailAlreadyExists);
            }

            var existingUserName = await _userManager.FindByNameAsync(request.Username);

            if (existingUserName is not null)
            {
                throw new InvalidOperationException(ErrorMessages.UsernameAlreadyExists);
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = false,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            EnsureSucceeded(createResult);

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Customer);

            EnsureSucceeded(roleResult);

            var customer = new Customer
            {
                UserId = user.Id,
                DefaultAddress = null
            };

            await _customerRepository.AddAsync(customer);

            await _unitOfWork.SaveChangesAsync();

            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationUrl = BuildCallbackUrl(_emailOptions.ConfirmationUrl, user.Id, confirmationToken);

            await SendConfirmationEmailAsync(user.Email!, confirmationUrl);

            return await CreateAuthenticationResultAsync(user);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await GetUserByIdOrThrowAsync(request.UserId.ToString(), ErrorMessages.UserNotFound);

            var decodedToken = DecodeToken(request.Token);

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            EnsureSucceeded(result);
        }

        public async Task UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default)
        {
            var user = await GetUserByIdOrThrowAsync(userId.ToString(), ErrorMessages.UserNotFound);

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;

            var result = await _userManager.UpdateAsync(user);
            EnsureSucceeded(result);
        }

        public async Task<UserInfoDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return null;
            }

            return new UserInfoDto(
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email,
                user.PhoneNumber);
        }

        private async Task<ApplicationUser> GetUserByIdOrThrowAsync(string userId, string errorMessage)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                throw new UnauthorizedAccessException(errorMessage);
            }
            return user;
        }

        private async Task<RefreshToken> GetStoredRefreshTokenOrThrowAsync(string refreshToken, string errorMessage)
        {
            var specification = new RefreshTokenByTokenSpecification(refreshToken);
            var storedToken = await _refreshTokenRepository.FirstOrDefaultAsync(specification);
            if (storedToken is null)
            {
                throw new UnauthorizedAccessException(errorMessage);
            }
            return storedToken;
        }

        private static string DecodeToken(string token)
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }

        private static string EncodeToken(string token)
        {
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        }

        private string BuildCallbackUrl(string baseUrl, Guid userId, string token)
        {
            var encodedToken = EncodeToken(token);
            return $"{baseUrl}?userId={userId}&token={encodedToken}";
        }

        private static void EnsureSucceeded(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
            }
        }

        private async Task SendConfirmationEmailAsync(string email, string confirmationUrl)
        {
            await _emailSender.SendEmailAsync(
                email,
                "Confirm your email",
                $"""
                <h2>Welcome to Logistics System</h2>

                <p>Please confirm your email by clicking the link below.</p>

                <a href="{confirmationUrl}">
                    Confirm Email
                </a>
                """);
        }

        private async Task SendResetPasswordEmailAsync(string email, string resetUrl)
        {
            await _emailSender.SendEmailAsync(
                email,
                "Reset your password",
                $"""
                <h2>Password Reset</h2>

                <p>Click the link below to reset your password.</p>

                <a href="{resetUrl}">
                    Reset Password
                </a>
                """);
        }

        private async Task<AuthenticationResult> CreateAuthenticationResultAsync(ApplicationUser user, RefreshToken? refreshToken = null)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var jwtUser = new JwtUser
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList()
            };

            var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(jwtUser);

            if (refreshToken is null)
            {
                refreshToken = await CreateRefreshTokenAsync(user);
            }

            return new AuthenticationResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),

                Email = user.Email!,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed
            };
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user)
        {
            var refreshToken = _refreshTokenGenerator.Generate(
                user.Id,
                _jwtOptions.RefreshTokenExpirationDays);

            await _refreshTokenRepository.AddAsync(refreshToken);

            await _unitOfWork.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(
            int pageNumber,
            int pageSize,
            string? role,
            bool? isActive,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query = _userManager.Users.AsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToHashSet();
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = user.ProfileImageUrl,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }

            return new PagedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<UserDetailsDto?> GetUserDetailsByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Customer)
                .Include(u => u.Driver)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDetailsDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                CustomerId = user.Customer?.Id,
                DriverId = user.Driver?.Id
            };
        }

        public async Task<UserDetailsDto> UpdateUserByAdminAsync(
            Guid userId,
            string firstName,
            string lastName,
            string? phoneNumber,
            string email,
            string userName,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                throw new KeyNotFoundException(ErrorMessages.UserNotFound);
            }

            var normalizedEmail = email.Trim();
            var normalizedUserName = userName.Trim();

            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existingByEmail = await _userManager.FindByEmailAsync(normalizedEmail);
                if (existingByEmail != null && existingByEmail.Id != user.Id)
                {
                    throw new InvalidOperationException(ErrorMessages.EmailAlreadyExists);
                }
            }

            if (!string.Equals(user.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
            {
                var existingByName = await _userManager.FindByNameAsync(normalizedUserName);
                if (existingByName != null && existingByName.Id != user.Id)
                {
                    throw new InvalidOperationException(ErrorMessages.UsernameAlreadyExists);
                }
            }

            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();
            user.PhoneNumber = phoneNumber?.Trim();
            user.Email = normalizedEmail;
            user.UserName = normalizedUserName;

            var updateResult = await _userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult);

            return (await GetUserDetailsByIdAsync(user.Id, cancellationToken))!;
        }

        public async Task SetUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                throw new KeyNotFoundException(ErrorMessages.UserNotFound);
            }

            user.IsActive = isActive;

            if (!isActive)
            {
                var tokens = await _refreshTokenRepository.ListAsync(
                    new ActiveRefreshTokensByUserSpecification(user.Id));
                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    _refreshTokenRepository.Update(token);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            EnsureSucceeded(result);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeactivateOrDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await SetUserStatusAsync(userId, false, cancellationToken);
        }
    }
}

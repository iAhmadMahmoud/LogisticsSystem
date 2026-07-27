using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Application.Specifications;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Authentication.Email;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using LogisticsSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Text;


namespace LogisticsSystem.Infrastructure.Authentication.Identity
{
    public sealed class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IGenericRepository<RefreshToken> _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtOptions _jwtOptions;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IEmailSender _emailSender;
        private readonly EmailOptions _emailOptions;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenGenerator jwtTokenGenerator,
            IUnitOfWork unitOfWork,
            IOptions<JwtOptions> jwtOptions,
            IGenericRepository<Customer> customerRepository,
            IRefreshTokenGenerator refreshTokenGenerator,
            IGenericRepository<RefreshToken> refreshTokenRepository,
            IEmailSender emailSender,
            IOptions<EmailOptions> emailOptions)
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
        }

        public Task ChangePasswordAsync(ChangePasswordRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
            }
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
                return;

            if (!user.EmailConfirmed)
                return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetUrl =$"{_emailOptions.ResetPasswordUrl}" + $"?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(
               user.Email!,
               "Reset your password",
               $"""
                <h2>Password Reset</h2>

                <p>Click the link below to reset your password.</p>

                <a href="{resetUrl}">
                    Reset Password
                </a>
                """);
        }

        public async Task<AuthenticationResult> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Your account has been deactivated.");
            }

            if (!user.EmailConfirmed)
            {
                throw new UnauthorizedAccessException("Please confirm your email before logging in.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            user.LastLoginAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", updateResult.Errors.Select(x => x.Description)));
            }

            return await CreateAuthenticationResultAsync(user);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var specfication = new RefreshTokenByTokenSpecification(refreshToken);

            var storedToken = await _refreshTokenRepository.FirstOrDefaultAsync(specfication);

            if(storedToken is null)
            {
                throw new UnauthorizedAccessException("Invaild refresh token.");
            }

            if (storedToken.IsRevoked)
                return;

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            _refreshTokenRepository.Update(storedToken);

            await _unitOfWork.SaveChangesAsync();
            
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            var specification = new RefreshTokenByTokenSpecification(refreshToken);

            var storedToken = await _refreshTokenRepository.FirstOrDefaultAsync(specification);
            
            if(storedToken is null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (!storedToken.IsActive)
            {
                throw new UnauthorizedAccessException("Refresh token is no longer valid.");
            }

            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());

            if (user is null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is inactive.");
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
            // Check Email
            var existingUser = await _userManager.FindByEmailAsync(request.Email);

            if (existingUser is not null)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            // Check Username
            var existingUserName = await _userManager.FindByNameAsync(request.Username);

            if (existingUserName is not null)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            // Create Identity User
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

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            // Assign Customer Role
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Customer);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            // Create Customer Domain Entity
            var customer = new Customer
            {
                UserId = user.Id,
                DefaultAddress = null
            };

            await _customerRepository.AddAsync(customer);

            await _unitOfWork.SaveChangesAsync();

            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));

            var confirmationUrl = $"{_emailOptions.ConfirmationUrl}?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Confirm your email",
                $"""
                <h2>Welcome to Logistics System</h2>

                <p>Please confirm your email by clicking the link below.</p>

                <a href="{confirmationUrl}">
                    Confirm Email
                </a>
                """);

            return await CreateAuthenticationResultAsync(user);
        }
        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user is null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(x => x.Description)));
            }
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

    }
}

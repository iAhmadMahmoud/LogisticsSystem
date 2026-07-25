using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using LogisticsSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LogisticsSystem.Infrastructure.Authentication.Identity
{
    public sealed class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtOptions _jwtOptions;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenGenerator jwtTokenGenerator,
            IUnitOfWork unitOfWork,
            IOptions<JwtOptions> jwtOptions,
            IGenericRepository<Customer> customerRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _unitOfWork = unitOfWork;
            _jwtOptions = jwtOptions.Value;
            _customerRepository = customerRepository;
        }

        public Task ChangePasswordAsync(ChangePasswordRequest request)
        {
            throw new NotImplementedException();
        }

        public Task ConfirmEmailAsync(string userId, string token)
        {
            throw new NotImplementedException();
        }

        public Task ForgotPasswordAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> LoginAsync(LoginRequest request)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            throw new NotImplementedException();
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

            // Get Roles
            var roles = await _userManager.GetRolesAsync(user);

            // Create JWT User
            var jwtUser = new JwtUser
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList()
            };

            // Generate Access Token
            var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(jwtUser);

            return new AuthenticationResult
            {
                AccessToken = accessToken,
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),

                EmailConfirmed = user.EmailConfirmed,
                UserName = user.UserName!,
                Email = user.Email!
            };
        }

        public Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            throw new NotImplementedException();
        }
    }
}

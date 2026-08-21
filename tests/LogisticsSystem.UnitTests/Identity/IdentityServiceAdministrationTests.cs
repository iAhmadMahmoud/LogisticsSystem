using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Exceptions;
using LogisticsSystem.Infrastructure.Authentication.Email;
using LogisticsSystem.Infrastructure.Authentication.Identity;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Identity
{
    public class IdentityServiceAdministrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IdentityService _identityService;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IGenericRepository<RefreshToken>> _refreshTokenRepoMock = new();
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtGeneratorMock = new();
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock = new();
        private readonly Mock<IEmailSender> _emailSenderMock = new();

        public IdentityServiceAdministrationTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"IdentityAdminTests_{Guid.NewGuid()}"));

            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            _roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            _identityService = new IdentityService(
                _userManager,
                _roleManager,
                _jwtGeneratorMock.Object,
                _unitOfWorkMock.Object,
                Options.Create(new JwtOptions()),
                _customerRepoMock.Object,
                _refreshTokenGeneratorMock.Object,
                _refreshTokenRepoMock.Object,
                _emailSenderMock.Object,
                Options.Create(new EmailOptions()),
                _currentUserServiceMock.Object,
                _dbContext);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            _serviceProvider.Dispose();
        }

        [Fact]
        public async Task CreateRoleAsync_WhenRoleIsUnique_CreatesAndReturnsRoleDto()
        {
            // Act
            var result = await _identityService.CreateRoleAsync("Supervisor", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Supervisor");
            result.IsSystemRole.Should().BeFalse();

            var roleExists = await _roleManager.RoleExistsAsync("Supervisor");
            roleExists.Should().BeTrue();
        }

        [Fact]
        public async Task CreateRoleAsync_WhenRoleAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = "Supervisor" });

            // Act
            var act = async () => await _identityService.CreateRoleAsync("Supervisor", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task DeleteRoleAsync_WhenRoleNotFound_ThrowsKeyNotFoundException()
        {
            // Act
            var act = async () => await _identityService.DeleteRoleAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Role not found.");
        }

        [Fact]
        public async Task DeleteRoleAsync_WhenRoleIsSystemRole_ThrowsDomainException()
        {
            // Arrange
            var adminRole = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = Roles.Admin, NormalizedName = Roles.Admin.ToUpperInvariant() };
            await _roleManager.CreateAsync(adminRole);

            // Act
            var act = async () => await _identityService.DeleteRoleAsync(adminRole.Id, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Cannot delete system roles.");
        }

        [Fact]
        public async Task DeleteRoleAsync_WhenRoleHasUsersAssigned_ThrowsDomainException()
        {
            // Arrange
            var customRole = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "WarehouseManager", NormalizedName = "WAREHOUSEMANAGER" };
            await _roleManager.CreateAsync(customRole);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "manager1",
                Email = "manager1@test.com",
                FirstName = "Manager",
                LastName = "One"
            };
            await _userManager.CreateAsync(user, "Password123!");
            await _userManager.AddToRoleAsync(user, "WarehouseManager");

            // Act
            var act = async () => await _identityService.DeleteRoleAsync(customRole.Id, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Cannot delete a role that is currently assigned to users.");
        }

        [Fact]
        public async Task DeleteRoleAsync_WhenRoleIsCustomAndUnassigned_DeletesSuccessfully()
        {
            // Arrange
            var customRole = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Auditor", NormalizedName = "AUDITOR" };
            await _roleManager.CreateAsync(customRole);

            // Act
            await _identityService.DeleteRoleAsync(customRole.Id, CancellationToken.None);

            // Assert
            var role = await _roleManager.FindByIdAsync(customRole.Id.ToString());
            role.Should().BeNull();
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
        {
            // Act
            var act = async () => await _identityService.AssignRoleToUserAsync(Guid.NewGuid(), "Admin", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WhenRoleNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user1",
                Email = "user1@test.com",
                FirstName = "User",
                LastName = "One"
            };
            await _userManager.CreateAsync(user, "Password123!");

            // Act
            var act = async () => await _identityService.AssignRoleToUserAsync(user.Id, "NonExistentRole", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Role not found.");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WhenValid_AssignsRoleToUser()
        {
            // Arrange
            await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = "Support", NormalizedName = "SUPPORT" });

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "supportuser",
                Email = "support@test.com",
                FirstName = "Support",
                LastName = "User"
            };
            await _userManager.CreateAsync(user, "Password123!");

            // Act
            await _identityService.AssignRoleToUserAsync(user.Id, "Support", CancellationToken.None);

            // Assert
            var isInRole = await _userManager.IsInRoleAsync(user, "Support");
            isInRole.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveRoleFromUserAsync_WhenValid_RemovesRoleFromUser()
        {
            // Arrange
            await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = "Support", NormalizedName = "SUPPORT" });

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "supportuser2",
                Email = "support2@test.com",
                FirstName = "Support",
                LastName = "User Two"
            };
            await _userManager.CreateAsync(user, "Password123!");
            await _userManager.AddToRoleAsync(user, "Support");

            // Act
            await _identityService.RemoveRoleFromUserAsync(user.Id, "Support", CancellationToken.None);

            // Assert
            var isInRole = await _userManager.IsInRoleAsync(user, "Support");
            isInRole.Should().BeFalse();
        }

        [Fact]
        public async Task SetUserStatusAsync_WhenDeactivated_SetsIsActiveFalseAndRevokesRefreshTokens()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "deactuser",
                Email = "deact@test.com",
                FirstName = "Deact",
                LastName = "User",
                IsActive = true
            };
            await _userManager.CreateAsync(user, "Password123!");

            var activeToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "token-abc",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _refreshTokenRepoMock.Setup(x => x.ListAsync(
                    It.IsAny<LogisticsSystem.Application.Common.Specifications.ISpecification<RefreshToken>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RefreshToken> { activeToken });

            // Act
            await _identityService.SetUserStatusAsync(user.Id, false, CancellationToken.None);

            // Assert
            var updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
            updatedUser!.IsActive.Should().BeFalse();

            activeToken.IsRevoked.Should().BeTrue();
            _refreshTokenRepoMock.Verify(x => x.Update(activeToken), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

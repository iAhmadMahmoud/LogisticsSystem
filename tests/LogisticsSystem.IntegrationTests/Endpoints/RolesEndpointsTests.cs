using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Roles;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class RolesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RolesEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetRoles_WithAdminRole_ReturnsAllRoles()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.GetAsync("/api/Roles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoleDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Should().Contain(r => r.Name == Roles.Admin);
            result.Should().Contain(r => r.Name == Roles.Dispatcher);
            result.Should().Contain(r => r.Name == Roles.Driver);
            result.Should().Contain(r => r.Name == Roles.Customer);
        }

        [Fact]
        public async Task CreateRole_WithUniqueNameAndAdmin_ReturnsCreatedAndPersists()
        {
            // Arrange
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var roleName = $"Manager_{Guid.NewGuid():N}";
            var request = new CreateRoleRequest(roleName);

            // Act
            var response = await client.PostAsJsonAsync("/api/Roles", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<RoleDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Name.Should().Be(roleName);
            result.IsSystemRole.Should().BeFalse();

            using var scope = _factory.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var exists = await roleManager.RoleExistsAsync(roleName);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task CreateRole_WhenRoleAlreadyExists_ReturnsConflict()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var request = new CreateRoleRequest(Roles.Admin);

            // Act
            var response = await client.PostAsJsonAsync("/api/Roles", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task DeleteRole_WhenSystemRole_ReturnsUnprocessableEntity()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            using var scope = _factory.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var adminRole = await roleManager.FindByNameAsync(Roles.Admin);
            adminRole.Should().NotBeNull();

            // Act
            var response = await client.DeleteAsync($"/api/Roles/{adminRole!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task DeleteRole_WhenCustomRoleUnassigned_DeletesAndReturnsNoContent()
        {
            // Arrange
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var customRoleName = $"TempRole_{Guid.NewGuid():N}";
            Guid roleId;
            using (var scope = _factory.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var role = new IdentityRole<Guid>(customRoleName);
                await roleManager.CreateAsync(role);
                roleId = role.Id;
            }

            // Act
            var response = await client.DeleteAsync($"/api/Roles/{roleId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyRoleManager = verifyScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var exists = await verifyRoleManager.RoleExistsAsync(customRoleName);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task AssignRole_WhenValidUserAndRole_AssignsRoleAndReturnsNoContent()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (user, _) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"role_assign_{Guid.NewGuid():N}@test.com");

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var request = new AssignRoleRequest(Roles.Dispatcher);

            // Act
            var response = await client.PostAsJsonAsync($"/api/Roles/users/{user.Id}", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userFromDb = await userManager.FindByIdAsync(user.Id.ToString());
            var inRole = await userManager.IsInRoleAsync(userFromDb!, Roles.Dispatcher);
            inRole.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveRole_WhenValidUserAndRole_RemovesRoleAndReturnsNoContent()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (user, _) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"role_rem_{Guid.NewGuid():N}@test.com");

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var u = await userManager.FindByIdAsync(user.Id.ToString());
                await userManager.AddToRoleAsync(u!, Roles.Dispatcher);
            }

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.DeleteAsync($"/api/Roles/users/{user.Id}/{Roles.Dispatcher}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userFromDb = await verifyUserManager.FindByIdAsync(user.Id.ToString());
            var inRole = await verifyUserManager.IsInRoleAsync(userFromDb!, Roles.Dispatcher);
            inRole.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveRole_WhenAdminRemovesAdminFromSelf_ReturnsUnprocessableEntity()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var adminId = Guid.NewGuid();
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                adminId,
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.DeleteAsync($"/api/Roles/users/{adminId}/{Roles.Admin}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task GetRoles_WithCustomerRole_ReturnsForbidden()
        {
            // Arrange
            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Customer);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            // Act
            var response = await client.GetAsync("/api/Roles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetRoles_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Roles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}

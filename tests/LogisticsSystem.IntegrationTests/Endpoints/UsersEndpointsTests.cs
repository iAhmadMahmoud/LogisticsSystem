using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Users;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class UsersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UsersEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetUsers_WithAdminRole_ReturnsPagedResult()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                adminId,
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.GetAsync("/api/Users?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetUsers_WithCustomerRole_ReturnsForbidden()
        {
            // Arrange
            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Customer);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            // Act
            var response = await client.GetAsync("/api/Users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUserById_WhenExists_ReturnsOkWithUserDetails()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"user_{Guid.NewGuid():N}@test.com");

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.GetAsync($"/api/Users/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<UserDetailsDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task GetUserById_WhenDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.GetAsync($"/api/Users/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateUser_WithAdminRole_UpdatesUserAndReturnsOk()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"update_target_{Guid.NewGuid():N}@test.com");

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var newEmail = $"updated_{Guid.NewGuid():N}@test.com";
            var request = new UpdateUserRequest(
                FirstName: "UpdatedFirst",
                LastName: "UpdatedLast",
                PhoneNumber: "+15551234567",
                Email: newEmail,
                UserName: $"upd_{Guid.NewGuid():N}");

            // Act
            var response = await client.PutAsJsonAsync($"/api/Users/{user.Id}", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<UserDetailsDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.FirstName.Should().Be("UpdatedFirst");
            result.LastName.Should().Be("UpdatedLast");
            result.Email.Should().Be(newEmail);
        }

        [Fact]
        public async Task UpdateUserStatus_WhenAdminDeactivatesAnotherUser_ReturnsNoContentAndDeactivates()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"deact_{Guid.NewGuid():N}@test.com");

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var request = new UpdateUserStatusRequest(false);

            // Act
            var response = await client.PatchAsJsonAsync($"/api/Users/{user.Id}/status", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
            updatedUser.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateUserStatus_WhenAdminDeactivatesSelf_ReturnsUnprocessableEntity()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                adminId,
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var request = new UpdateUserStatusRequest(false);

            // Act
            var response = await client.PatchAsJsonAsync($"/api/Users/{adminId}/status", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task DeleteUser_WhenAdminDeletesAnotherUser_ReturnsNoContentAndDeactivates()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"del_target_{Guid.NewGuid():N}@test.com");

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.DeleteAsync($"/api/Users/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
            updatedUser.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteUser_WhenAdminDeletesSelf_ReturnsUnprocessableEntity()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                adminId,
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.DeleteAsync($"/api/Users/{adminId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }
}

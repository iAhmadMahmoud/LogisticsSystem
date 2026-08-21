using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverLocation;
using LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverStatus;
using LogisticsSystem.Application.Features.Drivers.Queries.GetAllDrivers;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class DriversExtendedEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DriversExtendedEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UpdateDriverStatus_WhenCalledByDriver_UpdatesStatusInDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_status_{Guid.NewGuid()}@test.com", status: DriverStatus.Offline);

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, user.UserName!, Roles.Driver);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            var command = new UpdateDriverStatusCommand(DriverStatus.Available);

            // Act
            var response = await client.PatchAsJsonAsync("/api/Drivers/status", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database state
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var updatedDriver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
                updatedDriver.Should().NotBeNull();
                updatedDriver!.Status.Should().Be(DriverStatus.Available);
            }
        }

        [Fact]
        public async Task UpdateDriverLocation_WhenCalledByDriver_UpdatesCoordinatesInDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_loc_{Guid.NewGuid()}@test.com");

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, user.UserName!, Roles.Driver);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            var command = new UpdateDriverLocationCommand(30.1234, 31.5678);

            // Act
            var response = await client.PatchAsJsonAsync("/api/Drivers/location", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database state
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var updatedDriver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
                updatedDriver.Should().NotBeNull();
                updatedDriver!.Latitude.Should().Be(30.1234);
                updatedDriver.Longitude.Should().Be(31.5678);
            }
        }

        [Fact]
        public async Task GetAllDrivers_WhenAdmin_ReturnsPagedDrivers()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"drv_admin_{Guid.NewGuid()}@test.com");
            await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_list1_{Guid.NewGuid()}@test.com", status: DriverStatus.Available);

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.GetAsync("/api/Drivers?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResult<DriverListItemResponse>>(json, TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().NotBeEmpty();
        }
    }
}

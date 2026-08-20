using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Drivers;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class DriverVehicleAssignmentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DriverVehicleAssignmentEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AssignVehicle_WithValidAvailableVehicleAndDispatcher_ReturnsNoContentAndAssigns()
        {
            // Arrange
            var (_, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            var vehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"PL-{Guid.NewGuid():N}");

            var dispatcherUserId = Guid.NewGuid();
            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                dispatcherUserId,
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            var request = new AssignVehicleRequest(vehicle.Id);

            // Act
            var response = await client.PostAsJsonAsync($"/api/Drivers/{driver.Id}/vehicle", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedDriver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driver.Id);
            updatedDriver.Should().NotBeNull();
            updatedDriver!.VehicleId.Should().Be(vehicle.Id);
        }

        [Fact]
        public async Task AssignVehicle_WhenVehicleDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var (_, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);
            var nonExistentVehicleId = Guid.NewGuid();
            var request = new AssignVehicleRequest(nonExistentVehicleId);

            // Act
            var response = await client.PostAsJsonAsync($"/api/Drivers/{driver.Id}/vehicle", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AssignVehicle_WhenDriverDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var vehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"PL-{Guid.NewGuid():N}");

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);
            var nonExistentDriverId = Guid.NewGuid();
            var request = new AssignVehicleRequest(vehicle.Id);

            // Act
            var response = await client.PostAsJsonAsync($"/api/Drivers/{nonExistentDriverId}/vehicle", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AssignVehicle_WhenVehicleAlreadyAssignedToAnotherDriver_ReturnsUnprocessableEntity()
        {
            // Arrange
            var vehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"PL-{Guid.NewGuid():N}");

            var (_, driver1) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver1_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            var (_, driver2) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver2_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            // Assign vehicle to driver1 in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var d1 = await db.Drivers.FirstAsync(d => d.Id == driver1.Id);
                d1.VehicleId = vehicle.Id;
                await db.SaveChangesAsync();
            }

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            // Try to assign the same vehicle to driver2
            var request = new AssignVehicleRequest(vehicle.Id);

            // Act
            var response = await client.PostAsJsonAsync($"/api/Drivers/{driver2.Id}/vehicle", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task AssignVehicle_WhenDriverAlreadyHasAVehicle_ReturnsUnprocessableEntity()
        {
            // Arrange
            var vehicle1 = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"PL1-{Guid.NewGuid():N}");

            var vehicle2 = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"PL2-{Guid.NewGuid():N}");

            var (_, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            // Assign vehicle1 to driver
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var d = await db.Drivers.FirstAsync(x => x.Id == driver.Id);
                d.VehicleId = vehicle1.Id;
                await db.SaveChangesAsync();
            }

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            // Try to assign vehicle2 without unassigning vehicle1
            var request = new AssignVehicleRequest(vehicle2.Id);

            // Act
            var response = await client.PostAsJsonAsync($"/api/Drivers/{driver.Id}/vehicle", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task RemoveVehicle_WhenDriverHasAssignedVehicle_ReturnsNoContentAndUnassigns()
        {
            // Arrange
            var vehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"PL-{Guid.NewGuid():N}");

            var (_, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var d = await db.Drivers.FirstAsync(x => x.Id == driver.Id);
                d.VehicleId = vehicle.Id;
                await db.SaveChangesAsync();
            }

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            // Act
            var response = await client.DeleteAsync($"/api/Drivers/{driver.Id}/vehicle");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedDriver = await verifyDb.Drivers.AsNoTracking().FirstAsync(x => x.Id == driver.Id);
            updatedDriver.VehicleId.Should().BeNull();
        }

        [Fact]
        public async Task RemoveVehicle_WhenDriverHasNoVehicle_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (_, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            // Act
            var response = await client.DeleteAsync($"/api/Drivers/{driver.Id}/vehicle");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task AssignVehicle_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new AssignVehicleRequest(Guid.NewGuid());

            // Act
            var response = await client.PostAsJsonAsync($"/api/Drivers/{Guid.NewGuid()}/vehicle", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AssignVehicle_WithCustomerRole_ReturnsForbidden()
        {
            // Arrange
            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Customer);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);
            var request = new AssignVehicleRequest(Guid.NewGuid());

            // Act
            var response = await client.PostAsJsonAsync($"/api/Drivers/{Guid.NewGuid()}/vehicle", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RemoveVehicle_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.DeleteAsync($"/api/Drivers/{Guid.NewGuid()}/vehicle");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RemoveVehicle_WithCustomerRole_ReturnsForbidden()
        {
            // Arrange
            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Customer);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            // Act
            var response = await client.DeleteAsync($"/api/Drivers/{Guid.NewGuid()}/vehicle");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}

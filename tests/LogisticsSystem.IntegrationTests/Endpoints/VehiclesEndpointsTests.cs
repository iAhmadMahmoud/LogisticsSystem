using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Vehicles;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
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
    public class VehiclesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public VehiclesEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateVehicle_WithDispatcherRole_ReturnsCreatedAndPersists()
        {
            // Arrange
            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            var plateNumber = $"PL-{Guid.NewGuid():N}";
            var request = new CreateVehicleRequest(
                PlateNumber: plateNumber,
                Brand: "Toyota",
                Model: "Hilux",
                ManufacturingYear: 2023,
                Color: "Blue",
                Type: VehicleType.Truck,
                Capacity: 1500);

            // Act
            var response = await client.PostAsJsonAsync("/api/Vehicles", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<VehicleDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.PlateNumber.Should().Be(plateNumber.ToUpperInvariant());
            result.Brand.Should().Be("Toyota");
            result.Type.Should().Be(VehicleType.Truck);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vehicleInDb = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == result.Id);
            vehicleInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task GetVehicleById_WhenExists_ReturnsOk()
        {
            // Arrange
            var vehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"PL-{Guid.NewGuid():N}");

            var userToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Driver);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, userToken);

            // Act
            var response = await client.GetAsync($"/api/Vehicles/{vehicle.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<VehicleDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Id.Should().Be(vehicle.Id);
            result.PlateNumber.Should().Be(vehicle.PlateNumber);
        }

        [Fact]
        public async Task GetAvailableVehicles_ReturnsOnlyActiveAndUnassignedVehicles()
        {
            // Arrange
            var availableVehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"AVAIL-{Guid.NewGuid():N}",
                isActive: true);

            var inactiveVehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"INACT-{Guid.NewGuid():N}",
                isActive: false);

            var assignedVehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"ASSIGN-{Guid.NewGuid():N}",
                isActive: true);

            var (_, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"DL-{Guid.NewGuid():N}");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var d = await db.Drivers.FirstAsync(x => x.Id == driver.Id);
                d.VehicleId = assignedVehicle.Id;
                await db.SaveChangesAsync();
            }

            var token = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.GetAsync("/api/Vehicles/available?pageNumber=1&pageSize=50");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().Contain(v => v.Id == availableVehicle.Id);
            result.Items.Should().NotContain(v => v.Id == inactiveVehicle.Id);
            result.Items.Should().NotContain(v => v.Id == assignedVehicle.Id);
        }

        [Fact]
        public async Task DeleteVehicle_WhenAssignedToDriver_ReturnsUnprocessableEntity()
        {
            // Arrange
            var vehicle = await TestAuthHelper.SeedVehicleAsync(
                _factory.Services,
                plateNumber: $"DEL-{Guid.NewGuid():N}");

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

            var token = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.DeleteAsync($"/api/Vehicles/{vehicle.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }
}

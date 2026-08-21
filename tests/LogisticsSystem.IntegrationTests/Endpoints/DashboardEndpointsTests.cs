using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class DashboardEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DashboardEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetShipmentMetrics_WithDispatcherRole_ReturnsOkAndValidMetrics()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"dash_disp_{Guid.NewGuid():N}@test.com");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Shipments.Add(new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Status = ShipmentStatus.Pending,
                    PickupAddress = "Cairo",
                    DeliveryAddress = "Alexandria",
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/shipments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ShipmentDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.TotalShipments.Should().BeGreaterThanOrEqualTo(1);
            result.PendingShipments.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetShipmentMetrics_WithAdminRole_ReturnsOkAndValidMetrics()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/shipments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ShipmentDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetShipmentMetrics_WithCustomerRole_ReturnsForbidden()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Customer);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/shipments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetShipmentMetrics_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Dashboard/shipments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDriverMetrics_WithDispatcherRole_ReturnsOkAndValidMetrics()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (user, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"dash_driver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"LIC-{Guid.NewGuid():N}");

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/drivers");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DriverDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.TotalDrivers.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetDriverMetrics_WithAdminRole_ReturnsOkAndValidMetrics()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/drivers");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DriverDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDriverMetrics_WithCustomerRole_ReturnsForbidden()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Customer);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/drivers");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetDriverMetrics_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Dashboard/drivers");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetRecentActivity_WithDispatcherRole_ReturnsOkAndPaginatedResults()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"act_disp_{Guid.NewGuid():N}@test.com");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var shipment = new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Status = ShipmentStatus.Pending,
                    TrackingNumber = $"TRK-{Guid.NewGuid():N}",
                    PickupAddress = "Cairo",
                    DeliveryAddress = "Giza",
                    CreatedAt = DateTime.UtcNow
                };

                db.Shipments.Add(shipment);
                db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    Status = ShipmentStatus.Pending,
                    ChangedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Dispatcher);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/recent-activity?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResult<RecentActivityDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetRecentActivity_WithActivityTypeFilter_ReturnsFilteredResults()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Admin);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/recent-activity?activityType=ShipmentCreated");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResult<RecentActivityDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().OnlyContain(a => a.ActivityType == "ShipmentCreated");
        }

        [Fact]
        public async Task GetRecentActivity_WithCustomerRole_ReturnsForbidden()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                Guid.NewGuid(),
                role: Roles.Customer);

            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            // Act
            var response = await client.GetAsync("/api/Dashboard/recent-activity");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetRecentActivity_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Dashboard/recent-activity");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}

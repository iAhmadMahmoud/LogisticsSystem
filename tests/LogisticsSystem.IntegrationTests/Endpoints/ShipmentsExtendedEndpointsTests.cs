using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Shipments;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
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
    public class ShipmentsExtendedEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ShipmentsExtendedEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ShipmentLifecycle_PickupThenTransitThenDeliver_PersistsStateInDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"life_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"life_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Busy);

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.Assigned,
                trackingNumber: $"TRK-LIFE-{Guid.NewGuid():N}"[..12]);

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // 1. Pickup
            var pickupRes = await client.PostAsync($"/api/Shipments/{shipment.Id}/pickup", null);
            pickupRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s.Should().NotBeNull();
                s!.Status.Should().Be(ShipmentStatus.PickedUp);
                s.PickedUpAt.Should().NotBeNull();
            }

            // 2. Start Transit
            var transitRes = await client.PostAsync($"/api/Shipments/{shipment.Id}/start-transit", null);
            transitRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s.Should().NotBeNull();
                s!.Status.Should().Be(ShipmentStatus.InTransit);
            }

            // 3. Add Location
            var locRequest = new AddShipmentLocationRequest { Latitude = 30.05, Longitude = 31.25 };
            var locRes = await client.PostAsJsonAsync($"/api/Shipments/{shipment.Id}/location", locRequest);
            locRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 4. Deliver
            var deliverRes = await client.PostAsync($"/api/Shipments/{shipment.Id}/deliver", null);
            deliverRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s.Should().NotBeNull();
                s!.Status.Should().Be(ShipmentStatus.Delivered);
                s.DeliveredAt.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GetTrackingAndLatestLocation_ReturnsPersistedLocations()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"track_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"track_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, driverId: driver.Id, status: ShipmentStatus.InTransit);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.ShipmentTrackings.AddRange(
                    new ShipmentTracking { Id = Guid.NewGuid(), ShipmentId = shipment.Id, Latitude = 30.01, Longitude = 31.01, RecordedAt = DateTime.UtcNow.AddMinutes(-5) },
                    new ShipmentTracking { Id = Guid.NewGuid(), ShipmentId = shipment.Id, Latitude = 30.05, Longitude = 31.05, RecordedAt = DateTime.UtcNow }
                );
                await db.SaveChangesAsync();
            }

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act - Latest Location
            var latestRes = await client.GetAsync($"/api/Shipments/{shipment.Id}/location/latest");
            latestRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var latestJson = await latestRes.Content.ReadAsStringAsync();
            var latestDto = JsonSerializer.Deserialize<ShipmentTrackingDto>(latestJson, TestAuthHelper.JsonOptions);
            latestDto.Should().NotBeNull();
            latestDto!.Latitude.Should().Be(30.05);

            // Act - Tracking History
            var trackingRes = await client.GetAsync($"/api/Shipments/{shipment.Id}/tracking?pageNumber=1&pageSize=10");
            trackingRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var trackingJson = await trackingRes.Content.ReadAsStringAsync();
            var trackingResult = JsonSerializer.Deserialize<PagedResult<ShipmentTrackingDto>>(trackingJson, TestAuthHelper.JsonOptions);
            trackingResult.Should().NotBeNull();
            trackingResult!.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetStatusHistory_ReturnsChronologicalHistory()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"hist_admin_{Guid.NewGuid()}@test.com");
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, Guid.NewGuid(), status: ShipmentStatus.Delivered);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.ShipmentStatusHistories.AddRange(
                    new ShipmentStatusHistory { Id = Guid.NewGuid(), ShipmentId = shipment.Id, Status = ShipmentStatus.Pending, ChangedAt = DateTime.UtcNow.AddHours(-2) },
                    new ShipmentStatusHistory { Id = Guid.NewGuid(), ShipmentId = shipment.Id, Status = ShipmentStatus.InTransit, ChangedAt = DateTime.UtcNow.AddHours(-1) },
                    new ShipmentStatusHistory { Id = Guid.NewGuid(), ShipmentId = shipment.Id, Status = ShipmentStatus.Delivered, ChangedAt = DateTime.UtcNow }
                );
                await db.SaveChangesAsync();
            }

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.GetAsync($"/api/Shipments/{shipment.Id}/status-history");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            var histories = JsonSerializer.Deserialize<List<ShipmentStatusHistoryDto>>(json, TestAuthHelper.JsonOptions);
            histories.Should().NotBeNull();
            histories!.Should().HaveCount(3);
            histories![0].Status.Should().Be(ShipmentStatus.Pending);
            histories![2].Status.Should().Be(ShipmentStatus.Delivered);
        }

        [Fact]
        public async Task CancelShipment_WhenPending_CancelsAndUpdatesDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cancel_admin_{Guid.NewGuid()}@test.com");
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cancel_cust_{Guid.NewGuid()}@test.com");
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.PostAsync($"/api/Shipments/{shipment.Id}/cancel", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s.Should().NotBeNull();
                s!.Status.Should().Be(ShipmentStatus.Cancelled);
                s.CancelledAt.Should().NotBeNull();
            }
        }
    }
}

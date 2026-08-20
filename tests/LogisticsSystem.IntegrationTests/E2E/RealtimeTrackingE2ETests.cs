using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Shipments;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.E2E
{
    public class RealtimeTrackingE2ETests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RealtimeTrackingE2ETests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HubConnection CreateHubConnection(string token)
        {
            return new HubConnectionBuilder()
                .WithUrl(
                    new Uri(_factory.Server.BaseAddress, "/hubs/tracking"),
                    options =>
                    {
                        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                        options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    })
                .Build();
        }

        [Fact]
        public async Task RealtimeTracking_EndToEnd_CustomerReceivesLocationAndStatusUpdates()
        {
            // 1. Arrange - Seed Customer, Driver, and Shipment in Transit
            var (customerUser, customer) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"customer_{Guid.NewGuid()}@test.com");

            var (driverUser, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_{Guid.NewGuid()}@test.com",
                status: DriverStatus.Busy);

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customerId: customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.InTransit,
                trackingNumber: "TRK-E2E-REALTIME-001");

            // 2. Generate JWT tokens for Customer and Driver
            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                customerUser.Id,
                role: Roles.Customer);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(
                _factory.Services,
                driverUser.Id,
                role: Roles.Driver);

            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // 3. Customer connects to TrackingHub and subscribes to shipment
            await using var customerConnection = CreateHubConnection(customerToken);

            var locationReceivedTcs = new TaskCompletionSource<LocationUpdatedPayload>();
            customerConnection.On<LocationUpdatedPayload>("LocationUpdated", payload =>
            {
                locationReceivedTcs.TrySetResult(payload);
            });

            var statusReceivedTcs = new TaskCompletionSource<StatusChangedPayload>();
            customerConnection.On<StatusChangedPayload>("ShipmentStatusChanged", payload =>
            {
                statusReceivedTcs.TrySetResult(payload);
            });

            await customerConnection.StartAsync();
            customerConnection.State.Should().Be(HubConnectionState.Connected);

            await customerConnection.InvokeAsync("SubscribeToShipment", shipment.Id);

            // 4. Driver sends location update via HTTP API
            var locationRequest = new AddShipmentLocationRequest
            {
                Latitude = 30.0444,
                Longitude = 31.2357
            };

            var locationResponse = await driverClient.PostAsJsonAsync(
                $"/api/Shipments/{shipment.Id}/location",
                locationRequest);

            // Assert HTTP response
            locationResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database state for Location
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tracking = await db.ShipmentTrackings
                    .FirstOrDefaultAsync(t => t.ShipmentId == shipment.Id);

                tracking.Should().NotBeNull();
                tracking!.Latitude.Should().Be(30.0444);
                tracking.Longitude.Should().Be(31.2357);

                var updatedDriver = await db.Drivers.FindAsync(driver.Id);
                updatedDriver.Should().NotBeNull();
                updatedDriver!.Latitude.Should().Be(30.0444);
                updatedDriver.Longitude.Should().Be(31.2357);
            }

            // Assert Customer receives LocationUpdated event via SignalR
            var locationEvent = await locationReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            locationEvent.ShipmentId.Should().Be(shipment.Id);
            locationEvent.DriverId.Should().Be(driver.Id);
            locationEvent.Latitude.Should().Be(30.0444);
            locationEvent.Longitude.Should().Be(31.2357);

            // 5. Driver updates shipment status to Delivered via HTTP API
            var deliverResponse = await driverClient.PostAsync(
                $"/api/Shipments/{shipment.Id}/deliver",
                null);

            // Assert HTTP response
            deliverResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database state for Status
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var updatedShipment = await db.Shipments.FindAsync(shipment.Id);
                updatedShipment.Should().NotBeNull();
                updatedShipment!.Status.Should().Be(ShipmentStatus.Delivered);

                var updatedDriver = await db.Drivers.FindAsync(driver.Id);
                updatedDriver.Should().NotBeNull();
                updatedDriver!.Status.Should().Be(DriverStatus.Available);
            }

            // Assert Customer receives ShipmentStatusChanged event via SignalR
            var statusEvent = await statusReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            statusEvent.ShipmentId.Should().Be(shipment.Id);
            statusEvent.Status.Should().Be(nameof(ShipmentStatus.Delivered));

            await customerConnection.StopAsync();
        }

        private sealed record LocationUpdatedPayload(
            Guid ShipmentId,
            Guid DriverId,
            double Latitude,
            double Longitude,
            DateTime RecordedAt);

        private sealed record StatusChangedPayload(
            Guid ShipmentId,
            string Status,
            DateTime ChangedAt,
            string? Notes);
    }
}

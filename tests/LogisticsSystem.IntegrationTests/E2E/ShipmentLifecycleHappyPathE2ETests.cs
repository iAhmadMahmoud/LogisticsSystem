using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Shipments;
using LogisticsSystem.Application.Authentication.Commands.Register;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs;
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
    public class ShipmentLifecycleHappyPathE2ETests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ShipmentLifecycleHappyPathE2ETests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HubConnection CreateTrackingHubConnection(string token)
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

        private HubConnection CreateNotificationHubConnection(string token)
        {
            return new HubConnectionBuilder()
                .WithUrl(
                    new Uri(_factory.Server.BaseAddress, "/hubs/notifications"),
                    options =>
                    {
                        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                        options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    })
                .Build();
        }

        [Fact]
        public async Task CompleteShipmentLifecycle_HappyPath_FromRegistrationToDelivery_VerifiesAllStateAndRealtimeEvents()
        {
            // 0. Ensure system roles exist
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var anonymousClient = _factory.CreateClient();

            // 1. Customer registers via API
            var customerEmail = $"happy_cust_{Guid.NewGuid():N}@test.com";
            var registerRequest = new RegisterRequest
            {
                FirstName = "Happy",
                LastName = "Customer",
                Username = $"cust_{Guid.NewGuid():N}",
                Email = customerEmail,
                Password = "Password123!"
            };

            var registerResponse = await anonymousClient.PostAsJsonAsync("/api/Auth/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResult>(TestAuthHelper.JsonOptions);
            authResult.Should().NotBeNull();
            authResult!.AccessToken.Should().NotBeNullOrWhiteSpace();

            var customerToken = authResult.AccessToken;
            var customerClient = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            // 2. Customer connects to Notification Hub and Tracking Hub
            await using var notificationConnection = CreateNotificationHubConnection(customerToken);
            var receivedNotifications = new List<NotificationPayload>();
            notificationConnection.On<NotificationPayload>("NotificationReceived", payload =>
            {
                lock (receivedNotifications)
                {
                    receivedNotifications.Add(payload);
                }
            });

            await notificationConnection.StartAsync();
            notificationConnection.State.Should().Be(HubConnectionState.Connected);

            await using var trackingConnection = CreateTrackingHubConnection(customerToken);
            var receivedStatusChanges = new List<StatusChangedPayload>();
            var receivedLocations = new List<LocationUpdatedPayload>();

            trackingConnection.On<StatusChangedPayload>("ShipmentStatusChanged", payload =>
            {
                lock (receivedStatusChanges)
                {
                    receivedStatusChanges.Add(payload);
                }
            });

            trackingConnection.On<LocationUpdatedPayload>("LocationUpdated", payload =>
            {
                lock (receivedLocations)
                {
                    receivedLocations.Add(payload);
                }
            });

            await trackingConnection.StartAsync();
            trackingConnection.State.Should().Be(HubConnectionState.Connected);

            // 3. Customer creates a Shipment
            var createCommand = new CreateShipmentCommand(new CreateShipmentDto
            {
                PickupAddress = "100 Nile Corniche, Cairo, Egypt",
                PickupLatitude = 30.0444,
                PickupLongitude = 31.2357,
                DeliveryAddress = "200 Pyramids St, Giza, Egypt",
                DeliveryLatitude = 29.9792,
                DeliveryLongitude = 31.1342,
                Weight = 25.5m,
                DistanceKm = 15.0m,
                ShippingCost = 250.0m,
                Priority = ShipmentPriority.High,
                ScheduledAt = DateTime.UtcNow.AddHours(3)
            });

            var createResponse = await customerClient.PostAsJsonAsync("/api/Shipments", createCommand);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>(TestAuthHelper.JsonOptions);
            createdShipment.Should().NotBeNull();
            createdShipment!.Status.Should().Be(ShipmentStatus.Pending);
            createdShipment.TrackingNumber.Should().NotBeNullOrWhiteSpace();

            var shipmentId = createdShipment.Id;

            // Subscribe customer to shipment tracking updates
            await trackingConnection.InvokeAsync("SubscribeToShipment", shipmentId);

            // 4. Dispatcher views pending shipment and assigns an available driver
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"disp_happy_{Guid.NewGuid():N}@test.com");
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var dispatcherClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);

            var getPendingResponse = await dispatcherClient.GetAsync($"/api/Shipments/{shipmentId}");
            getPendingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver_happy_{Guid.NewGuid():N}@test.com",
                status: DriverStatus.Available);

            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            var assignPayload = new AssignDriverRequest { DriverId = driver.Id };
            var assignResponse = await dispatcherClient.PostAsJsonAsync($"/api/Shipments/{shipmentId}/assign-driver", assignPayload);
            assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 5. Driver queries assignment offer and accepts
            Guid assignmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var assignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipmentId && a.DriverId == driver.Id && a.Status == AssignmentStatus.Pending);
                assignment.Should().NotBeNull();
                assignmentId = assignment!.Id;
            }

            var acceptResponse = await driverClient.PostAsync($"/api/Dispatch/assignments/{assignmentId}/accept", null);
            acceptResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 6. Driver picks up shipment
            var pickupResponse = await driverClient.PostAsync($"/api/Shipments/{shipmentId}/pickup", null);
            pickupResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 7. Driver starts transit
            var transitResponse = await driverClient.PostAsync($"/api/Shipments/{shipmentId}/start-transit", null);
            transitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 8. Driver sends GPS location update
            var locationPayload = new AddShipmentLocationRequest
            {
                Latitude = 30.0125,
                Longitude = 31.1850
            };
            var locationResponse = await driverClient.PostAsJsonAsync($"/api/Shipments/{shipmentId}/location", locationPayload);
            locationResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 9. Driver delivers shipment
            var deliverResponse = await driverClient.PostAsync($"/api/Shipments/{shipmentId}/deliver", null);
            deliverResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Small delay to allow async SignalR events to process
            await Task.Delay(500);

            // 10. Verification of HTTP Query APIs
            // 10.1 Verify Shipment Details
            var getFinalShipmentResponse = await customerClient.GetAsync($"/api/Shipments/{shipmentId}");
            getFinalShipmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var finalShipmentDto = await getFinalShipmentResponse.Content.ReadFromJsonAsync<ShipmentDto>(TestAuthHelper.JsonOptions);
            finalShipmentDto.Should().NotBeNull();
            finalShipmentDto!.Status.Should().Be(ShipmentStatus.Delivered);
            finalShipmentDto.DriverId.Should().Be(driver.Id);
            finalShipmentDto.DeliveredAt.Should().NotBeNull();
            finalShipmentDto.PickedUpAt.Should().NotBeNull();
            finalShipmentDto.AssignedAt.Should().NotBeNull();

            // 10.2 Verify Shipment Status History timeline
            var historyResponse = await customerClient.GetAsync($"/api/Shipments/{shipmentId}/status-history");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var historyList = await historyResponse.Content.ReadFromJsonAsync<List<ShipmentStatusHistoryDto>>(TestAuthHelper.JsonOptions);
            historyList.Should().NotBeNull();
            historyList!.Should().HaveCount(5, "all 5 lifecycle transitions must be recorded chronologically");

            var statusSequence = historyList!.Select(h => h.Status).ToList();
            statusSequence.Should().ContainInOrder(
                ShipmentStatus.Pending,
                ShipmentStatus.Assigned,
                ShipmentStatus.PickedUp,
                ShipmentStatus.InTransit,
                ShipmentStatus.Delivered);

            // 10.3 Verify Assignment History
            var assignHistoryResponse = await customerClient.GetAsync($"/api/Shipments/{shipmentId}/assignments/history?pageNumber=1&pageSize=10");
            assignHistoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 10.4 Verify Customer Notifications via API
            var notifResponse = await customerClient.GetAsync("/api/Notifications?pageNumber=1&pageSize=50");
            notifResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var notifPaged = await notifResponse.Content.ReadFromJsonAsync<PagedResult<NotificationResponse>>(TestAuthHelper.JsonOptions);
            notifPaged.Should().NotBeNull();
            notifPaged!.Items.Should().Contain(n => n.Type == NotificationType.ShipmentAssigned);
            notifPaged!.Items.Should().Contain(n => n.Type == NotificationType.ShipmentPickedUp);
            notifPaged!.Items.Should().Contain(n => n.Type == NotificationType.ShipmentInTransit);
            notifPaged!.Items.Should().Contain(n => n.Type == NotificationType.ShipmentDelivered);

            // 11. Verification of Database State
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dbShipment = await db.Shipments.FindAsync(shipmentId);
                dbShipment.Should().NotBeNull();
                dbShipment!.Status.Should().Be(ShipmentStatus.Delivered);
                dbShipment.DriverId.Should().Be(driver.Id);
                dbShipment.DeliveredAt.Should().NotBeNull();

                var dbDriver = await db.Drivers.FindAsync(driver.Id);
                dbDriver.Should().NotBeNull();
                dbDriver!.Status.Should().Be(DriverStatus.Available, "driver should be freed after delivery");
                dbDriver.Latitude.Should().Be(30.0125);
                dbDriver.Longitude.Should().Be(31.1850);

                var dbAssignment = await db.DispatchAssignments.FindAsync(assignmentId);
                dbAssignment.Should().NotBeNull();
                dbAssignment!.Status.Should().Be(AssignmentStatus.Accepted);
                dbAssignment.RespondedAt.Should().NotBeNull();

                var dbTrackings = await db.ShipmentTrackings.Where(t => t.ShipmentId == shipmentId).ToListAsync();
                dbTrackings.Should().NotBeEmpty();
                dbTrackings.Should().Contain(t => Math.Abs(t.Latitude - 30.0125) < 0.0001 && Math.Abs(t.Longitude - 31.1850) < 0.0001);
            }

            // 12. Verification of Realtime SignalR Events
            lock (receivedStatusChanges)
            {
                receivedStatusChanges.Should().Contain(s => s.ShipmentId == shipmentId && s.Status == nameof(ShipmentStatus.PickedUp));
                receivedStatusChanges.Should().Contain(s => s.ShipmentId == shipmentId && s.Status == nameof(ShipmentStatus.InTransit));
                receivedStatusChanges.Should().Contain(s => s.ShipmentId == shipmentId && s.Status == nameof(ShipmentStatus.Delivered));
            }

            lock (receivedLocations)
            {
                receivedLocations.Should().Contain(l => l.ShipmentId == shipmentId && Math.Abs(l.Latitude - 30.0125) < 0.0001);
            }

            // Clean up connections
            await notificationConnection.StopAsync();
            await trackingConnection.StopAsync();
        }

        private sealed record NotificationPayload(
            Guid Id,
            string Title,
            string Message,
            string Type,
            bool IsRead,
            DateTime CreatedAt);

        private sealed record StatusChangedPayload(
            Guid ShipmentId,
            string Status,
            DateTime ChangedAt,
            string? Notes);

        private sealed record LocationUpdatedPayload(
            Guid ShipmentId,
            Guid DriverId,
            double Latitude,
            double Longitude,
            DateTime RecordedAt);
    }
}

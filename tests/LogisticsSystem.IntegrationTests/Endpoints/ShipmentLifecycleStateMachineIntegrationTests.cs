using System.Net;
using FluentAssertions;
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
    public class ShipmentLifecycleStateMachineIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ShipmentLifecycleStateMachineIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task FullStateMachine_PendingToAssignedToAcceptedToPickedUpToInTransitToDelivered()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"fsm_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"fsm_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Available);

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                status: ShipmentStatus.Pending,
                trackingNumber: $"TRK-FSM-{Guid.NewGuid():N}"[..12]);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            var assignmentId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.DispatchAssignments.Add(new DispatchAssignment
                {
                    Id = assignmentId,
                    ShipmentId = shipment.Id,
                    DriverId = driver.Id,
                    AttemptNumber = 1,
                    Status = AssignmentStatus.Pending,
                    SentAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // Step 1: Driver Accepts Assignment -> Status becomes Assigned, Driver becomes Busy
            var acceptRes = await driverClient.PostAsync($"/api/Dispatch/assignments/{assignmentId}/accept", null);
            acceptRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s!.Status.Should().Be(ShipmentStatus.Assigned);
                s.DriverId.Should().Be(driver.Id);
                s.AssignedAt.Should().NotBeNull();

                var d = await db.Drivers.FirstOrDefaultAsync(x => x.Id == driver.Id);
                d!.Status.Should().Be(DriverStatus.Busy);
            }

            // Step 2: Driver Pickups Shipment -> Status becomes PickedUp
            var pickupRes = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/pickup", null);
            pickupRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s!.Status.Should().Be(ShipmentStatus.PickedUp);
                s.PickedUpAt.Should().NotBeNull();
            }

            // Step 3: Driver Starts Transit -> Status becomes InTransit
            var transitRes = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/start-transit", null);
            transitRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s!.Status.Should().Be(ShipmentStatus.InTransit);
            }

            // Step 4: Driver Delivers Shipment -> Status becomes Delivered, Driver becomes Available
            var deliverRes = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/deliver", null);
            deliverRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s!.Status.Should().Be(ShipmentStatus.Delivered);
                s.DeliveredAt.Should().NotBeNull();

                var d = await db.Drivers.FirstOrDefaultAsync(x => x.Id == driver.Id);
                d!.Status.Should().Be(DriverStatus.Available);

                // Verify Chronological Status History
                var history = await db.ShipmentStatusHistories
                    .Where(h => h.ShipmentId == shipment.Id)
                    .OrderBy(h => h.ChangedAt)
                    .ToListAsync();

                history.Should().HaveCountGreaterThanOrEqualTo(4);
                history.Select(h => h.Status).Should().ContainInOrder(
                    ShipmentStatus.Assigned,
                    ShipmentStatus.PickedUp,
                    ShipmentStatus.InTransit,
                    ShipmentStatus.Delivered);

                // Verify Notifications for Customer
                var notifications = await db.Notifications
                    .Where(n => n.UserId == custUser.Id)
                    .ToListAsync();

                notifications.Should().NotBeEmpty();
                notifications.Select(n => n.Type).Should().Contain(new[]
                {
                    NotificationType.ShipmentAssigned,
                    NotificationType.ShipmentDelivered
                });
            }
        }

        [Fact]
        public async Task AlternativePath_InTransitToFailed_UpdatesDatabaseCorrectly()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"fail_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"fail_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Busy);

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.InTransit);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // Act
            var failRes = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/fail", null);

            // Assert
            failRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var s = await db.Shipments.FirstOrDefaultAsync(x => x.Id == shipment.Id);
                s!.Status.Should().Be(ShipmentStatus.Failed);
                s.FailedAt.Should().NotBeNull();

                var d = await db.Drivers.FirstOrDefaultAsync(x => x.Id == driver.Id);
                d!.Status.Should().Be(DriverStatus.Available);
            }
        }

        [Fact]
        public async Task InvalidTransition_PendingToDelivered_ReturnsErrorResponse()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"inv_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"inv_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.Pending);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // Act - attempt illegal transition directly to Delivered
            var response = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/deliver", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
            ((int)response.StatusCode).Should().BeOneOf((int)HttpStatusCode.BadRequest, (int)HttpStatusCode.UnprocessableEntity, 500);
        }

        [Fact]
        public async Task InvalidTransition_DeliveredToPendingOrTransit_ReturnsErrorResponse()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"delv_inv_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"delv_inv_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.Delivered);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // Act - attempt illegal transition to Start Transit
            var response = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/start-transit", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task InvalidTransition_CancelledToTransit_ReturnsErrorResponse()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"canc_inv_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"canc_inv_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.Cancelled);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // Act - attempt illegal transition
            var response = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/pickup", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task InvalidTransition_FailedToDelivered_ReturnsErrorResponse()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"fail_inv_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"fail_inv_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.Failed);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // Act
            var response = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/deliver", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task DuplicateTransition_DuplicatePickup_ReturnsErrorResponse()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dup_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"dup_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Busy);

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.PickedUp);

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // Act - Pickup again
            var response = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/pickup", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task UnauthorizedTransition_CustomerCallingPickup_ReturnsForbidden()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"unauth_cust_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Assigned);

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);

            // Act
            var response = await custClient.PostAsync($"/api/Shipments/{shipment.Id}/pickup", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UnauthorizedTransition_DriverDeliveringOtherDriverShipment_ReturnsError()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"other_cust_{Guid.NewGuid()}@test.com");
            var (_, assignedDriver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"assigned_drv_{Guid.NewGuid()}@test.com");
            var (otherDrvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"other_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: assignedDriver.Id,
                status: ShipmentStatus.InTransit);

            var otherDriverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, otherDrvUser.Id, otherDrvUser.Email!, otherDrvUser.UserName!, Roles.Driver);
            var otherDriverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, otherDriverToken);

            // Act
            var response = await otherDriverClient.PostAsync($"/api/Shipments/{shipment.Id}/deliver", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
            ((int)response.StatusCode).Should().BeOneOf((int)HttpStatusCode.Forbidden, (int)HttpStatusCode.Unauthorized, 500);
        }
    }
}

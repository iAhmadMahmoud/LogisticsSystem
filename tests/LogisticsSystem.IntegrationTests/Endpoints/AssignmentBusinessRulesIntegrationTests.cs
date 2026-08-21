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
    public class AssignmentBusinessRulesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AssignmentBusinessRulesIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ShipmentCancellation_WithAssignedDriver_ReleasesDriverToAvailableInDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"canc_rel_admin_{Guid.NewGuid()}@test.com");
            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"canc_rel_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"canc_rel_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Busy);

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.Assigned);

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act - Cancel assigned shipment
            var response = await adminClient.PostAsync($"/api/Shipments/{shipment.Id}/cancel", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Shipment is Cancelled
                var updatedShipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == shipment.Id);
                updatedShipment.Should().NotBeNull();
                updatedShipment!.Status.Should().Be(ShipmentStatus.Cancelled);
                updatedShipment.CancelledAt.Should().NotBeNull();

                // Driver is released to Available
                var updatedDriver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
                updatedDriver.Should().NotBeNull();
                updatedDriver!.Status.Should().Be(DriverStatus.Available);

                // Both Customer and Driver received notifications
                var custNotif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == custUser.Id && n.Type == NotificationType.ShipmentCancelled);
                custNotif.Should().NotBeNull();

                var drvNotif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == drvUser.Id && n.Type == NotificationType.ShipmentCancelled);
                drvNotif.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task AssignmentRejection_WhenNoOtherDriverExists_EmitsNoDriverAvailableNotificationToCustomer()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"rej_nodrv_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"rej_nodrv_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Available, latitude: 15.001, longitude: 15.001);

            // Isolated location with no other drivers nearby
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending, pickupLatitude: 15.0, pickupLongitude: 15.0);

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

            var driverToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, driverToken);

            // Act - Only driver rejects
            var response = await driverClient.PostAsync($"/api/Dispatch/assignments/{assignmentId}/reject", null);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var assignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
                assignment.Should().NotBeNull();
                assignment!.Status.Should().Be(AssignmentStatus.Rejected);
            }
        }
    }
}

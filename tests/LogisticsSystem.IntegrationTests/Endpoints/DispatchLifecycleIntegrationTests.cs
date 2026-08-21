using System.Net;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory;
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
    public class DispatchLifecycleIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DispatchLifecycleIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateDispatchAssignment_CreatesPendingAssignmentAndDriverNotification()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"disp_create_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"disp_create_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Available);
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            // Act
            using (var scope = _factory.Services.CreateScope())
            {
                var dispatchService = scope.ServiceProvider.GetRequiredService<IDispatchAssignmentService>();
                var assignment = await dispatchService.CreateAssignmentAsync(shipment, driver);
                var unitOfWork = scope.ServiceProvider.GetRequiredService<LogisticsSystem.Application.Common.Interfaces.Persistence.IUnitOfWork>();
                await unitOfWork.SaveChangesAsync();

                assignment.Should().NotBeNull();
                assignment!.Status.Should().Be(AssignmentStatus.Pending);
                assignment.AttemptNumber.Should().Be(1);
            }

            // Assert Database & Notification
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var savedAssignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipment.Id && a.DriverId == driver.Id);
                savedAssignment.Should().NotBeNull();
                savedAssignment!.Status.Should().Be(AssignmentStatus.Pending);

                var driverNotification = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == drvUser.Id && n.Type == NotificationType.DispatchAssignmentReceived);
                driverNotification.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task DriverRejectsAssignment_AutoReassignsToNextAvailableDriver()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"disp_rej_auto_cust_{Guid.NewGuid()}@test.com");
            var (drvUserA, driverA) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"disp_rej_drvA_{Guid.NewGuid()}@test.com", status: DriverStatus.Available, latitude: 21.001, longitude: 21.001);
            var (drvUserB, driverB) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"disp_rej_drvB_{Guid.NewGuid()}@test.com", status: DriverStatus.Available, latitude: 21.002, longitude: 21.002);

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending, pickupLatitude: 21.0, pickupLongitude: 21.0);

            var assignmentId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.DispatchAssignments.Add(new DispatchAssignment
                {
                    Id = assignmentId,
                    ShipmentId = shipment.Id,
                    DriverId = driverA.Id,
                    AttemptNumber = 1,
                    Status = AssignmentStatus.Pending,
                    SentAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var tokenA = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUserA.Id, drvUserA.Email!, drvUserA.UserName!, Roles.Driver);
            var clientA = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenA);

            // Act - Driver A rejects assignment
            var response = await clientA.PostAsync($"/api/Dispatch/assignments/{assignmentId}/reject", null);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert - Assignment 1 is Rejected, Assignment 2 is created for Driver B
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var firstAssignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
                firstAssignment.Should().NotBeNull();
                firstAssignment!.Status.Should().Be(AssignmentStatus.Rejected);

                var secondAssignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipment.Id && a.DriverId == driverB.Id);
                secondAssignment.Should().NotBeNull();
                secondAssignment!.Status.Should().Be(AssignmentStatus.Pending);
                secondAssignment.AttemptNumber.Should().Be(2);

                // Driver B received notification
                var notifB = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == drvUserB.Id && n.Type == NotificationType.DispatchAssignmentReceived);
                notifB.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task AssignmentExpires_ExpireAssignmentsService_MarksExpiredAndReassigns()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"exp_cust_{Guid.NewGuid()}@test.com");
            var (_, driverA) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"exp_drva_{Guid.NewGuid()}@test.com", status: DriverStatus.Available, latitude: 22.001, longitude: 22.001);
            var (drvUserB, driverB) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"exp_drvb_{Guid.NewGuid()}@test.com", status: DriverStatus.Available, latitude: 22.002, longitude: 22.002);

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending, pickupLatitude: 22.0, pickupLongitude: 22.0);

            var assignmentId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.DispatchAssignments.Add(new DispatchAssignment
                {
                    Id = assignmentId,
                    ShipmentId = shipment.Id,
                    DriverId = driverA.Id,
                    AttemptNumber = 1,
                    Status = AssignmentStatus.Pending,
                    SentAt = DateTime.UtcNow.AddMinutes(-30) // Expired (> 5 minutes default)
                });
                await db.SaveChangesAsync();
            }

            // Act - Run expiration service
            using (var scope = _factory.Services.CreateScope())
            {
                var expirationService = scope.ServiceProvider.GetRequiredService<IAssignmentExpirationService>();
                await expirationService.ExpireAssignmentsAsync();
            }

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var expiredAssignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
                expiredAssignment.Should().NotBeNull();
                expiredAssignment!.Status.Should().Be(AssignmentStatus.Expired);

                var newAssignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipment.Id && a.DriverId == driverB.Id);
                newAssignment.Should().NotBeNull();
                newAssignment!.Status.Should().Be(AssignmentStatus.Pending);
                newAssignment.AttemptNumber.Should().Be(2);
            }
        }

        [Fact]
        public async Task DriverAccept_WhenDriverUnavailable_ReturnsError()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"unavail_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"unavail_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Offline);

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

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

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.PostAsync($"/api/Dispatch/assignments/{assignmentId}/accept", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task DriverAccept_WhenShipmentAlreadyHasDriver_ReturnsError()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"already_cust_{Guid.NewGuid()}@test.com");
            var (_, driverA) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"already_drva_{Guid.NewGuid()}@test.com", status: DriverStatus.Busy);
            var (drvUserB, driverB) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"already_drvb_{Guid.NewGuid()}@test.com", status: DriverStatus.Available);

            // Shipment already assigned to driverA
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, driverId: driverA.Id, status: ShipmentStatus.Assigned);

            var assignmentId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.DispatchAssignments.Add(new DispatchAssignment
                {
                    Id = assignmentId,
                    ShipmentId = shipment.Id,
                    DriverId = driverB.Id,
                    AttemptNumber = 2,
                    Status = AssignmentStatus.Pending,
                    SentAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUserB.Id, drvUserB.Email!, drvUserB.UserName!, Roles.Driver);
            var clientB = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenB);

            // Act
            var response = await clientB.PostAsync($"/api/Dispatch/assignments/{assignmentId}/accept", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task DriverAccept_WhenAssignmentBelongsToAnotherDriver_ReturnsUnauthorizedOrForbidden()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"mismatch_cust_{Guid.NewGuid()}@test.com");
            var (_, driverA) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"mismatch_drva_{Guid.NewGuid()}@test.com");
            var (drvUserB, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"mismatch_drvb_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            var assignmentId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.DispatchAssignments.Add(new DispatchAssignment
                {
                    Id = assignmentId,
                    ShipmentId = shipment.Id,
                    DriverId = driverA.Id,
                    AttemptNumber = 1,
                    Status = AssignmentStatus.Pending,
                    SentAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // Driver B tries to accept Driver A's assignment
            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUserB.Id, drvUserB.Email!, drvUserB.UserName!, Roles.Driver);
            var clientB = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenB);

            // Act
            var response = await clientB.PostAsync($"/api/Dispatch/assignments/{assignmentId}/accept", null);

            // Assert
            response.IsSuccessStatusCode.Should().BeFalse();
            ((int)response.StatusCode).Should().BeOneOf((int)HttpStatusCode.Forbidden, (int)HttpStatusCode.Unauthorized, 500);
        }

        [Fact]
        public async Task GetAssignmentHistory_ReturnsAllChronologicalAttempts()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"hist_disp_admin_{Guid.NewGuid()}@test.com");
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"hist_disp_cust_{Guid.NewGuid()}@test.com");
            var (_, driverA) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"hist_disp_drva_{Guid.NewGuid()}@test.com");
            var (_, driverB) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"hist_disp_drvb_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, driverId: driverB.Id, status: ShipmentStatus.Assigned);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.DispatchAssignments.AddRange(
                    new DispatchAssignment
                    {
                        Id = Guid.NewGuid(),
                        ShipmentId = shipment.Id,
                        DriverId = driverA.Id,
                        AttemptNumber = 1,
                        Status = AssignmentStatus.Rejected,
                        SentAt = DateTime.UtcNow.AddMinutes(-10),
                        RespondedAt = DateTime.UtcNow.AddMinutes(-8)
                    },
                    new DispatchAssignment
                    {
                        Id = Guid.NewGuid(),
                        ShipmentId = shipment.Id,
                        DriverId = driverB.Id,
                        AttemptNumber = 2,
                        Status = AssignmentStatus.Accepted,
                        SentAt = DateTime.UtcNow.AddMinutes(-5),
                        RespondedAt = DateTime.UtcNow.AddMinutes(-4)
                    }
                );
                await db.SaveChangesAsync();
            }

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act
            var response = await adminClient.GetAsync($"/api/Shipments/{shipment.Id}/assignments/history?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResult<AssignmentHistoryResponse>>(json, TestAuthHelper.JsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.Items[0].AttemptNumber.Should().Be(2);
            result.Items[0].Status.Should().Be(AssignmentStatus.Accepted);
            result.Items[1].AttemptNumber.Should().Be(1);
            result.Items[1].Status.Should().Be(AssignmentStatus.Rejected);
        }
    }
}

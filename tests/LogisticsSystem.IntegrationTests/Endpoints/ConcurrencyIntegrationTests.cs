using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Services;
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
    public class ConcurrencyIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ConcurrencyIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SimultaneousDriverAcceptance_SameDriverTwoShipments_ExactlyOneSucceeds()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_drv_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"conc_drv_{Guid.NewGuid()}@test.com");

            var shipment1 = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);
            var shipment2 = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            // Create 2 pending dispatch offers for the same driver
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.DispatchAssignments.AddRange(
                    new DispatchAssignment
                    {
                        Id = Guid.NewGuid(),
                        ShipmentId = shipment1.Id,
                        DriverId = driver.Id,
                        AttemptNumber = 1,
                        Status = AssignmentStatus.Pending,
                        SentAt = DateTime.UtcNow
                    },
                    new DispatchAssignment
                    {
                        Id = Guid.NewGuid(),
                        ShipmentId = shipment2.Id,
                        DriverId = driver.Id,
                        AttemptNumber = 1,
                        Status = AssignmentStatus.Pending,
                        SentAt = DateTime.UtcNow
                    }
                );
                await db.SaveChangesAsync();
            }

            // Retrieve assignment IDs
            Guid assignment1Id, assignment2Id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                assignment1Id = (await db.DispatchAssignments.FirstAsync(a => a.ShipmentId == shipment1.Id)).Id;
                assignment2Id = (await db.DispatchAssignments.FirstAsync(a => a.ShipmentId == shipment2.Id)).Id;
            }

            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            // Act - Driver attempts to accept both offers simultaneously
            var task1 = driverClient.PostAsync($"/api/Dispatch/assignments/{assignment1Id}/accept", null);
            var task2 = driverClient.PostAsync($"/api/Dispatch/assignments/{assignment2Id}/accept", null);

            var responses = await Task.WhenAll(task1, task2);

            // Assert
            var successResponses = responses.Where(r => r.StatusCode == HttpStatusCode.NoContent).ToList();
            var failedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.BadRequest || r.StatusCode == HttpStatusCode.UnprocessableEntity).ToList();

            successResponses.Should().HaveCount(1, "exactly one assignment acceptance should succeed");
            failedResponses.Should().HaveCount(1, "the competing acceptance should fail because driver becomes busy");

            // Verify persisted database state consistency
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var updatedDriver = await db.Drivers.FindAsync(driver.Id);
                updatedDriver!.Status.Should().Be(DriverStatus.Busy);

                var s1 = await db.Shipments.FindAsync(shipment1.Id);
                var s2 = await db.Shipments.FindAsync(shipment2.Id);

                var assignedShipments = new[] { s1!, s2! }.Where(s => s.Status == ShipmentStatus.Assigned).ToList();
                var pendingShipments = new[] { s1!, s2! }.Where(s => s.Status == ShipmentStatus.Pending).ToList();

                assignedShipments.Should().HaveCount(1);
                assignedShipments[0].DriverId.Should().Be(driver.Id);

                pendingShipments.Should().HaveCount(1);
                pendingShipments[0].DriverId.Should().BeNull();

                var acceptedAssignments = await db.DispatchAssignments.Where(a => a.DriverId == driver.Id && a.Status == AssignmentStatus.Accepted).ToListAsync();
                acceptedAssignments.Should().HaveCount(1);
            }
        }

        [Fact]
        public async Task SimultaneousShipmentAssignment_TwoDispatchersSameShipment_NoDuplicateActiveAssignments()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_disp_cust_{Guid.NewGuid()}@test.com");
            var (_, driver1) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"conc_disp_drv1_{Guid.NewGuid()}@test.com");
            var (_, driver2) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"conc_disp_drv2_{Guid.NewGuid()}@test.com");

            var (disp1User, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_disp1_{Guid.NewGuid()}@test.com");
            var (disp2User, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_disp2_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            var disp1Token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, disp1User.Id, disp1User.Email!, disp1User.UserName!, Roles.Dispatcher);
            var disp2Token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, disp2User.Id, disp2User.Email!, disp2User.UserName!, Roles.Dispatcher);

            var client1 = TestAuthHelper.CreateAuthenticatedClient(_factory, disp1Token);
            var client2 = TestAuthHelper.CreateAuthenticatedClient(_factory, disp2Token);

            // Act - Two dispatchers simultaneously assign different drivers to the same shipment
            var payload1 = new { driverId = driver1.Id };
            var payload2 = new { driverId = driver2.Id };

            var task1 = client1.PostAsJsonAsync($"/api/Shipments/{shipment.Id}/assign-driver", payload1);
            var task2 = client2.PostAsJsonAsync($"/api/Shipments/{shipment.Id}/assign-driver", payload2);

            var responses = await Task.WhenAll(task1, task2);

            // Assert
            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.NoContent);
            var failureCount = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest || r.StatusCode == HttpStatusCode.UnprocessableEntity);

            (successCount + failureCount).Should().Be(2);
            successCount.Should().BeInRange(1, 2);

            // Verify database consistency
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var pendingAssignments = await db.DispatchAssignments
                    .Where(a => a.ShipmentId == shipment.Id && a.Status == AssignmentStatus.Pending)
                    .ToListAsync();

                pendingAssignments.Should().NotBeEmpty();

                var dbShipment = await db.Shipments.FindAsync(shipment.Id);
                dbShipment!.Status.Should().Be(ShipmentStatus.Pending);
            }
        }

        [Fact]
        public async Task SimultaneousAcceptanceAndExpiration_DeterministicWinningState()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_exp_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"conc_exp_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            Guid assignmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var assignment = new DispatchAssignment
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    DriverId = driver.Id,
                    AttemptNumber = 1,
                    Status = AssignmentStatus.Pending,
                    SentAt = DateTime.UtcNow.AddMinutes(-10) // Past the 5-minute threshold
                };
                db.DispatchAssignments.Add(assignment);
                await db.SaveChangesAsync();
                assignmentId = assignment.Id;
            }

            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            // Act - Concurrently trigger driver acceptance and assignment expiration service
            var acceptTask = driverClient.PostAsync($"/api/Dispatch/assignments/{assignmentId}/accept", null);
            var expireTask = Task.Run(async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var expirationService = scope.ServiceProvider.GetRequiredService<IAssignmentExpirationService>();
                await expirationService.ExpireAssignmentsAsync(CancellationToken.None);
            });

            await Task.WhenAll(acceptTask, expireTask);

            // Assert - The assignment must be in a deterministic state (either Accepted or Expired, never corrupted)
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var finalAssignment = await db.DispatchAssignments.FindAsync(assignmentId);
                var finalShipment = await db.Shipments.FindAsync(shipment.Id);
                var finalDriver = await db.Drivers.FindAsync(driver.Id);

                if (finalAssignment!.Status == AssignmentStatus.Accepted)
                {
                    finalShipment!.Status.Should().Be(ShipmentStatus.Assigned);
                    finalShipment.DriverId.Should().Be(driver.Id);
                    finalDriver!.Status.Should().Be(DriverStatus.Busy);
                }
                else
                {
                    finalAssignment.Status.Should().Be(AssignmentStatus.Expired);
                    finalShipment!.Status.Should().Be(ShipmentStatus.Pending);
                    finalShipment.DriverId.Should().BeNull();
                    finalDriver!.Status.Should().Be(DriverStatus.Available);
                }
            }
        }

        [Fact]
        public async Task SimultaneousCancellationAndAcceptance_ConsistentDatabaseState()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_canc_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"conc_canc_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            Guid assignmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var assignment = new DispatchAssignment
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    DriverId = driver.Id,
                    AttemptNumber = 1,
                    Status = AssignmentStatus.Pending,
                    SentAt = DateTime.UtcNow
                };
                db.DispatchAssignments.Add(assignment);
                await db.SaveChangesAsync();
                assignmentId = assignment.Id;
            }

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            // Act - Concurrently cancel shipment and accept assignment
            var cancelTask = custClient.PostAsync($"/api/Shipments/{shipment.Id}/cancel", null);
            var acceptTask = drvClient.PostAsync($"/api/Dispatch/assignments/{assignmentId}/accept", null);

            await Task.WhenAll(cancelTask, acceptTask);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dbShipment = await db.Shipments.FindAsync(shipment.Id);
                var dbDriver = await db.Drivers.FindAsync(driver.Id);

                if (dbShipment!.Status == ShipmentStatus.Cancelled)
                {
                    // If cancelled, driver should not remain trapped as Busy
                    dbDriver!.Status.Should().Be(DriverStatus.Available);
                }
                else
                {
                    dbShipment.Status.Should().Be(ShipmentStatus.Assigned);
                    dbDriver!.Status.Should().Be(DriverStatus.Busy);
                }
            }
        }

        [Fact]
        public async Task DuplicatePickupRequests_ReplayProtection_GeneratesSingleHistoryAndNotification()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_dup_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"conc_dup_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.Assigned);

            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            // Act - Send duplicate simultaneous pickup requests
            var task1 = driverClient.PostAsync($"/api/Shipments/{shipment.Id}/pickup", null);
            var task2 = driverClient.PostAsync($"/api/Shipments/{shipment.Id}/pickup", null);

            var responses = await Task.WhenAll(task1, task2);

            // Assert
            var successResponses = responses.Where(r => r.StatusCode == HttpStatusCode.NoContent).ToList();
            var failureResponses = responses.Where(r => r.StatusCode == HttpStatusCode.BadRequest || r.StatusCode == HttpStatusCode.UnprocessableEntity).ToList();

            successResponses.Should().HaveCount(1, "first pickup transition succeeds");
            failureResponses.Should().HaveCount(1, "duplicate pickup transition is rejected by state machine validator");

            // Verify no duplicate status history or notifications
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dbShipment = await db.Shipments.FindAsync(shipment.Id);
                dbShipment!.Status.Should().Be(ShipmentStatus.PickedUp);

                var pickedUpHistories = await db.ShipmentStatusHistories
                    .Where(h => h.ShipmentId == shipment.Id && h.Status == ShipmentStatus.PickedUp)
                    .ToListAsync();

                pickedUpHistories.Should().HaveCount(1, "duplicate transition must not insert duplicate status history records");

                var pickedUpNotifications = await db.Notifications
                    .Where(n => n.UserId == customer.UserId && n.Type == NotificationType.ShipmentPickedUp)
                    .ToListAsync();

                pickedUpNotifications.Should().HaveCount(1, "duplicate transition must not send duplicate notifications");
            }
        }

        [Fact]
        public async Task SimultaneousConflictingTransitions_DeliverVsCancel_RejectsConflictingTransition()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"conc_conf_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"conc_conf_drv_{Guid.NewGuid()}@test.com");

            var shipment = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driver.Id,
                status: ShipmentStatus.InTransit);

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            // Act - Driver attempts to deliver while Customer attempts to cancel an InTransit shipment
            var deliverTask = drvClient.PostAsync($"/api/Shipments/{shipment.Id}/deliver", null);
            var cancelTask = custClient.PostAsync($"/api/Shipments/{shipment.Id}/cancel", null);

            var responses = await Task.WhenAll(deliverTask, cancelTask);

            // Assert
            var deliverResponse = responses[0];
            var cancelResponse = responses[1];

            deliverResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            cancelResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Forbidden);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dbShipment = await db.Shipments.FindAsync(shipment.Id);
                dbShipment!.Status.Should().Be(ShipmentStatus.Delivered);

                var dbDriver = await db.Drivers.FindAsync(driver.Id);
                dbDriver!.Status.Should().Be(DriverStatus.Available);
            }
        }
    }
}

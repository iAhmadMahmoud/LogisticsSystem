using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Shipments;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.E2E
{
    public class ShipmentRejectionLifecycleE2ETests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ShipmentRejectionLifecycleE2ETests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ShipmentLifecycle_DriverRejectionWithAutoReassignment_CompletesSuccessfully()
        {
            // 0. Ensure roles seeded
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            // 1. Seed Customer, Dispatcher, Driver 1, and Driver 2
            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_rej_{Guid.NewGuid():N}@test.com");
            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var customerClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);

            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"disp_rej_{Guid.NewGuid():N}@test.com");
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var dispatcherClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);

            var (drv1User, driver1) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver1_rej_{Guid.NewGuid():N}@test.com",
                latitude: 30.0444,
                longitude: 31.2357,
                status: DriverStatus.Available);
            var drv1Token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drv1User.Id, drv1User.Email!, drv1User.UserName!, Roles.Driver);
            var driver1Client = TestAuthHelper.CreateAuthenticatedClient(_factory, drv1Token);

            var (drv2User, driver2) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver2_rej_{Guid.NewGuid():N}@test.com",
                latitude: 30.0450,
                longitude: 31.2360,
                status: DriverStatus.Available);
            var drv2Token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drv2User.Id, drv2User.Email!, drv2User.UserName!, Roles.Driver);
            var driver2Client = TestAuthHelper.CreateAuthenticatedClient(_factory, drv2Token);

            // 2. Customer creates Shipment (Pending)
            var createCommand = new CreateShipmentCommand(new CreateShipmentDto
            {
                PickupAddress = "Cairo Downtown",
                PickupLatitude = 30.0444,
                PickupLongitude = 31.2357,
                DeliveryAddress = "Giza Square",
                DeliveryLatitude = 30.0131,
                DeliveryLongitude = 31.2089,
                Weight = 10m,
                DistanceKm = 8m,
                ShippingCost = 100m,
                Priority = ShipmentPriority.Normal,
                ScheduledAt = DateTime.UtcNow.AddHours(1)
            });

            var createResponse = await customerClient.PostAsJsonAsync("/api/Shipments", createCommand);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>(TestAuthHelper.JsonOptions);
            createdShipment.Should().NotBeNull();
            var shipmentId = createdShipment!.Id;

            // 3. Dispatcher assigns Driver 1
            var assignResponse = await dispatcherClient.PostAsJsonAsync($"/api/Shipments/{shipmentId}/assign-driver", new AssignDriverRequest { DriverId = driver1.Id });
            assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify Assignment 1 is Pending for Driver 1
            Guid assignment1Id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var a1 = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipmentId && a.DriverId == driver1.Id && a.Status == AssignmentStatus.Pending);
                a1.Should().NotBeNull();
                assignment1Id = a1!.Id;
            }

            // 4. Driver 1 rejects the assignment
            var rejectResponse = await driver1Client.PostAsync($"/api/Dispatch/assignments/{assignment1Id}/reject", null);
            rejectResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 5. Verify Driver 1 assignment is Rejected and Driver 2 was automatically assigned (Attempt 2)
            Guid assignment2Id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var a1 = await db.DispatchAssignments.FindAsync(assignment1Id);
                a1.Should().NotBeNull();
                a1!.Status.Should().Be(AssignmentStatus.Rejected);
                a1.RespondedAt.Should().NotBeNull();

                var a2 = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipmentId && a.DriverId == driver2.Id && a.Status == AssignmentStatus.Pending);
                a2.Should().NotBeNull("the system must automatically reassign to the next available driver");
                a2!.AttemptNumber.Should().Be(2);
                assignment2Id = a2.Id;

                var shipment = await db.Shipments.FindAsync(shipmentId);
                shipment.Should().NotBeNull();
                shipment!.Status.Should().Be(ShipmentStatus.Pending, "shipment remains pending until accepted");
            }

            // 6. Driver 2 accepts the new assignment offer
            var acceptResponse = await driver2Client.PostAsync($"/api/Dispatch/assignments/{assignment2Id}/accept", null);
            acceptResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 7. Verify shipment is now Assigned to Driver 2
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var a2 = await db.DispatchAssignments.FindAsync(assignment2Id);
                a2.Should().NotBeNull();
                a2!.Status.Should().Be(AssignmentStatus.Accepted);

                var shipment = await db.Shipments.FindAsync(shipmentId);
                shipment.Should().NotBeNull();
                shipment!.Status.Should().Be(ShipmentStatus.Assigned);
                shipment.DriverId.Should().Be(driver2.Id);

                var dbDriver2 = await db.Drivers.FindAsync(driver2.Id);
                dbDriver2.Should().NotBeNull();
                dbDriver2!.Status.Should().Be(DriverStatus.Busy);
            }

            // 8. Driver 2 progresses shipment through Pickup -> Transit -> Delivery
            var pickupRes = await driver2Client.PostAsync($"/api/Shipments/{shipmentId}/pickup", null);
            pickupRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var transitRes = await driver2Client.PostAsync($"/api/Shipments/{shipmentId}/start-transit", null);
            transitRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var deliverRes = await driver2Client.PostAsync($"/api/Shipments/{shipmentId}/deliver", null);
            deliverRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 9. Verify Assignment History API
            var assignHistoryResponse = await customerClient.GetAsync($"/api/Shipments/{shipmentId}/assignments/history?pageNumber=1&pageSize=10");
            assignHistoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var assignHistory = await assignHistoryResponse.Content.ReadFromJsonAsync<PagedResult<AssignmentHistoryResponse>>(TestAuthHelper.JsonOptions);
            assignHistory.Should().NotBeNull();
            assignHistory!.Items.Should().HaveCount(2);

            var a1Dto = assignHistory.Items.FirstOrDefault(a => a.DriverId == driver1.Id);
            a1Dto.Should().NotBeNull();
            a1Dto!.Status.Should().Be(AssignmentStatus.Rejected);
            a1Dto.AttemptNumber.Should().Be(1);

            var a2Dto = assignHistory.Items.FirstOrDefault(a => a.DriverId == driver2.Id);
            a2Dto.Should().NotBeNull();
            a2Dto!.Status.Should().Be(AssignmentStatus.Accepted);
            a2Dto.AttemptNumber.Should().Be(2);

            // 10. Verify Status History API
            var statusHistoryResponse = await customerClient.GetAsync($"/api/Shipments/{shipmentId}/status-history");
            statusHistoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var statusHistory = await statusHistoryResponse.Content.ReadFromJsonAsync<List<ShipmentStatusHistoryDto>>(TestAuthHelper.JsonOptions);
            statusHistory.Should().NotBeNull();
            statusHistory!.Select(s => s.Status).Should().ContainInOrder(
                ShipmentStatus.Pending,
                ShipmentStatus.Assigned,
                ShipmentStatus.PickedUp,
                ShipmentStatus.InTransit,
                ShipmentStatus.Delivered);
        }

        [Fact]
        public async Task ShipmentLifecycle_DriverRejectionWithNoAlternativeDriver_FallsBackToCustomerAndAllowsManualDispatch()
        {
            // 0. Ensure roles seeded and isolate driver availability
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var existingDrivers = await db.Drivers.ToListAsync();
                foreach (var d in existingDrivers)
                {
                    d.Status = DriverStatus.Offline;
                }
                await db.SaveChangesAsync();
            }

            // 1. Seed Customer, Dispatcher, and only 1 Driver
            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_fallback_{Guid.NewGuid():N}@test.com");
            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var customerClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);

            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"disp_fallback_{Guid.NewGuid():N}@test.com");
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var dispatcherClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);

            var (drv1User, driver1) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver1_fallback_{Guid.NewGuid():N}@test.com",
                status: DriverStatus.Available);
            var drv1Token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drv1User.Id, drv1User.Email!, drv1User.UserName!, Roles.Driver);
            var driver1Client = TestAuthHelper.CreateAuthenticatedClient(_factory, drv1Token);

            // 2. Customer creates Shipment
            var createCommand = new CreateShipmentCommand(new CreateShipmentDto
            {
                PickupAddress = "Alexandria Port",
                PickupLatitude = 31.2001,
                PickupLongitude = 29.9187,
                DeliveryAddress = "Cairo Port",
                DeliveryLatitude = 30.0444,
                DeliveryLongitude = 31.2357,
                Weight = 50m,
                DistanceKm = 220m,
                ShippingCost = 1500m,
                Priority = ShipmentPriority.Express,
                ScheduledAt = DateTime.UtcNow.AddHours(2)
            });

            var createResponse = await customerClient.PostAsJsonAsync("/api/Shipments", createCommand);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>(TestAuthHelper.JsonOptions);
            createdShipment.Should().NotBeNull();
            var shipmentId = createdShipment!.Id;

            // 3. Dispatcher assigns Driver 1
            var assignResponse1 = await dispatcherClient.PostAsJsonAsync($"/api/Shipments/{shipmentId}/assign-driver", new AssignDriverRequest { DriverId = driver1.Id });
            assignResponse1.StatusCode.Should().Be(HttpStatusCode.NoContent);

            Guid assignment1Id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var a1 = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipmentId && a.DriverId == driver1.Id);
                a1.Should().NotBeNull();
                assignment1Id = a1!.Id;
            }

            // 4. Driver 1 rejects
            var rejectResponse = await driver1Client.PostAsync($"/api/Dispatch/assignments/{assignment1Id}/reject", null);
            rejectResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 5. Verify assignment is Rejected, customer received NoDriverAvailable notification, and shipment is still Pending
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var a1 = await db.DispatchAssignments.FindAsync(assignment1Id);
                a1.Should().NotBeNull();
                a1!.Status.Should().Be(AssignmentStatus.Rejected);

                var shipment = await db.Shipments.FindAsync(shipmentId);
                shipment.Should().NotBeNull();
                shipment!.Status.Should().Be(ShipmentStatus.Pending);
                shipment.DriverId.Should().BeNull();

                var customerNotifications = await db.Notifications.Where(n => n.UserId == custUser.Id).ToListAsync();
                customerNotifications.Should().Contain(n => n.Type == NotificationType.NoDriverAvailable);
            }

            // Verify customer notifications API
            var notifResponse = await customerClient.GetAsync("/api/Notifications?pageNumber=1&pageSize=50");
            notifResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var notifPaged = await notifResponse.Content.ReadFromJsonAsync<PagedResult<NotificationResponse>>(TestAuthHelper.JsonOptions);
            notifPaged.Should().NotBeNull();
            notifPaged!.Items.Should().Contain(n => n.Type == NotificationType.NoDriverAvailable);

            // 6. Now Driver 2 is registered and becomes available
            var (drv2User, driver2) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"driver2_fallback_{Guid.NewGuid():N}@test.com",
                status: DriverStatus.Available);
            var drv2Token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drv2User.Id, drv2User.Email!, drv2User.UserName!, Roles.Driver);
            var driver2Client = TestAuthHelper.CreateAuthenticatedClient(_factory, drv2Token);

            // 7. Dispatcher manually re-assigns Driver 2
            var assignResponse2 = await dispatcherClient.PostAsJsonAsync($"/api/Shipments/{shipmentId}/assign-driver", new AssignDriverRequest { DriverId = driver2.Id });
            assignResponse2.StatusCode.Should().Be(HttpStatusCode.NoContent);

            Guid assignment2Id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var a2 = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.ShipmentId == shipmentId && a.DriverId == driver2.Id && a.Status == AssignmentStatus.Pending);
                a2.Should().NotBeNull();
                assignment2Id = a2!.Id;
            }

            // 8. Driver 2 accepts
            var acceptResponse2 = await driver2Client.PostAsync($"/api/Dispatch/assignments/{assignment2Id}/accept", null);
            acceptResponse2.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 9. Verify shipment is now successfully Assigned to Driver 2
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var shipment = await db.Shipments.FindAsync(shipmentId);
                shipment.Should().NotBeNull();
                shipment!.Status.Should().Be(ShipmentStatus.Assigned);
                shipment.DriverId.Should().Be(driver2.Id);

                var a2 = await db.DispatchAssignments.FindAsync(assignment2Id);
                a2.Should().NotBeNull();
                a2!.Status.Should().Be(AssignmentStatus.Accepted);
            }
        }
    }
}

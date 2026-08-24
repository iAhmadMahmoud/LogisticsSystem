using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Users;
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
    public class AuthorizationMatrixIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthorizationMatrixIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UnauthenticatedRequests_Return401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act & Assert
            var shipmentRes = await client.PostAsJsonAsync("/api/Shipments", new { });
            shipmentRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var driversRes = await client.GetAsync("/api/Drivers");
            driversRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var usersRes = await client.GetAsync("/api/Users");
            usersRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var rolesRes = await client.GetAsync("/api/Roles");
            rolesRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var dashRes = await client.GetAsync("/api/Dashboard/recent-activity");
            dashRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ShipmentCreation_PermissionMatrix_AllowedForCustomerDispatcherAdmin_ForbiddenForDriver()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"auth_cust_{Guid.NewGuid()}@test.com");
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"auth_disp_{Guid.NewGuid()}@test.com");
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"auth_admin_{Guid.NewGuid()}@test.com");
            var (drvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"auth_drv_{Guid.NewGuid()}@test.com");

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var dispClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            var request = new
            {
                shipment = new
                {
                    pickupAddress = "Cairo",
                    pickupLatitude = 30.0,
                    pickupLongitude = 31.0,
                    deliveryAddress = "Alexandria",
                    deliveryLatitude = 31.0,
                    deliveryLongitude = 29.9,
                    weight = 10.0,
                    distanceKm = 200.0,
                    shippingCost = 150.0,
                    priority = (int)ShipmentPriority.Normal,
                    scheduledAt = DateTime.UtcNow.AddDays(1)
                }
            };

            // Act & Assert
            var custRes = await custClient.PostAsJsonAsync("/api/Shipments", request);
            custRes.StatusCode.Should().Be(HttpStatusCode.Created);

            var dispRes = await dispClient.PostAsJsonAsync("/api/Shipments", request);
            dispRes.StatusCode.Should().Be(HttpStatusCode.Created);

            var adminRes = await adminClient.PostAsJsonAsync("/api/Shipments", request);
            adminRes.StatusCode.Should().Be(HttpStatusCode.Created);

            var drvRes = await drvClient.PostAsJsonAsync("/api/Shipments", request);
            drvRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UserAndRoleManagement_PermissionMatrix_AllowedOnlyForAdmin()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"usr_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"usr_drv_{Guid.NewGuid()}@test.com");
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"usr_disp_{Guid.NewGuid()}@test.com");
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"usr_admin_{Guid.NewGuid()}@test.com");

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);
            var dispClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act & Assert - Users endpoint
            (await custClient.GetAsync("/api/Users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await drvClient.GetAsync("/api/Users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await dispClient.GetAsync("/api/Users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await adminClient.GetAsync("/api/Users")).StatusCode.Should().Be(HttpStatusCode.OK);

            // Act & Assert - Roles endpoint
            (await custClient.GetAsync("/api/Roles")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await drvClient.GetAsync("/api/Roles")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await dispClient.GetAsync("/api/Roles")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await adminClient.GetAsync("/api/Roles")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DriverManagement_PermissionMatrix_AllowedForDispatcherAndAdmin()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"drv_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_drv_{Guid.NewGuid()}@test.com");
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"drv_disp_{Guid.NewGuid()}@test.com");
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"drv_admin_{Guid.NewGuid()}@test.com");

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);
            var dispClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act & Assert
            (await custClient.GetAsync("/api/Drivers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await drvClient.GetAsync("/api/Drivers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await dispClient.GetAsync("/api/Drivers")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await adminClient.GetAsync("/api/Drivers")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DriverStatusUpdate_PermissionMatrix_AllowedOnlyForDriverRole()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dstat_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"dstat_drv_{Guid.NewGuid()}@test.com");
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dstat_disp_{Guid.NewGuid()}@test.com");
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dstat_admin_{Guid.NewGuid()}@test.com");

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);
            var dispClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var payload = new { status = (int)DriverStatus.Available };

            // Act & Assert
            (await custClient.PatchAsJsonAsync("/api/Drivers/status", payload)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await dispClient.PatchAsJsonAsync("/api/Drivers/status", payload)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await adminClient.PatchAsJsonAsync("/api/Drivers/status", payload)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await drvClient.PatchAsJsonAsync("/api/Drivers/status", payload)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Dashboard_PermissionMatrix_AllowedForDispatcherAndAdmin()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dash_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"dash_drv_{Guid.NewGuid()}@test.com");
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dash_disp_{Guid.NewGuid()}@test.com");
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dash_admin_{Guid.NewGuid()}@test.com");

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);
            var dispClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act & Assert
            (await custClient.GetAsync("/api/Dashboard/recent-activity")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await drvClient.GetAsync("/api/Dashboard/recent-activity")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await dispClient.GetAsync("/api/Dashboard/recent-activity")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await adminClient.GetAsync("/api/Dashboard/recent-activity")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ResourceOwnership_CustomerCannotAccessAnotherCustomersShipment()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (userA, customerA) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"owner_a_{Guid.NewGuid()}@test.com");
            var (userB, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"owner_b_{Guid.NewGuid()}@test.com");

            var shipmentA = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customerA.Id);

            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userB.Id, userB.Email!, userB.UserName!, Roles.Customer);
            var clientB = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenB);

            // Act - Customer B attempts to view Customer A's shipment
            var response = await clientB.GetAsync($"/api/Shipments/{shipmentA.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ResourceOwnership_DriverCannotModifyAnotherDriversShipment()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"drv_own_cust_{Guid.NewGuid()}@test.com");
            var (drvUserA, driverA) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_own_a_{Guid.NewGuid()}@test.com");
            var (drvUserB, driverB) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_own_b_{Guid.NewGuid()}@test.com");

            var shipmentAssignedToA = await TestAuthHelper.SeedShipmentAsync(
                _factory.Services,
                customer.Id,
                driverId: driverA.Id,
                status: ShipmentStatus.Assigned);

            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUserB.Id, drvUserB.Email!, drvUserB.UserName!, Roles.Driver);
            var clientB = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenB);

            // Act - Driver B attempts to pickup Driver A's assigned shipment
            var response = await clientB.PostAsync($"/api/Shipments/{shipmentAssignedToA.Id}/pickup", null);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task AdminSelfProtection_PreventSelfDeletionAndDeactivationViaApi()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"selfprot_admin_{Guid.NewGuid()}@test.com");

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act 1 - Admin attempts to delete own user account
            var deleteRes = await adminClient.DeleteAsync($"/api/Users/{adminUser.Id}");
            deleteRes.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            // Act 2 - Admin attempts to deactivate own user account
            var deactRes = await adminClient.PatchAsJsonAsync($"/api/Users/{adminUser.Id}/status", new UpdateUserStatusRequest(IsActive: false));
            deactRes.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task AdminSelfProtection_PreventRemovingAdminRoleFromOwnAccount()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"selfdemote_admin_{Guid.NewGuid()}@test.com");

            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Act - Admin attempts to remove Admin role from own account
            var removeRoleRes = await adminClient.DeleteAsync($"/api/Roles/users/{adminUser.Id}/{Roles.Admin}");

            // Assert
            removeRoleRes.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task VehicleManagement_PermissionMatrix_AllowedForDispatcherAndAdmin_ForbiddenForCustomerAndDriver()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"veh_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"veh_drv_{Guid.NewGuid()}@test.com");
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"veh_disp_{Guid.NewGuid()}@test.com");
            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"veh_admin_{Guid.NewGuid()}@test.com");

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, dispUser.Email!, dispUser.UserName!, Roles.Dispatcher);
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);
            var dispClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispToken);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var createVehiclePayload1 = new
            {
                plateNumber = $"PLT-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}",
                brand = "Mercedes",
                model = "Actros",
                manufacturingYear = 2023,
                color = "Blue",
                type = (int)VehicleType.Truck,
                capacity = 15000.0
            };

            var createVehiclePayload2 = new
            {
                plateNumber = $"PLT-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}",
                brand = "Volvo",
                model = "FH16",
                manufacturingYear = 2024,
                color = "Red",
                type = (int)VehicleType.Truck,
                capacity = 20000.0
            };

            // Act & Assert - Create Vehicle
            (await custClient.PostAsJsonAsync("/api/Vehicles", createVehiclePayload1)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await drvClient.PostAsJsonAsync("/api/Vehicles", createVehiclePayload1)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await dispClient.PostAsJsonAsync("/api/Vehicles", createVehiclePayload1)).StatusCode.Should().Be(HttpStatusCode.Created);
            (await adminClient.PostAsJsonAsync("/api/Vehicles", createVehiclePayload2)).StatusCode.Should().Be(HttpStatusCode.Created);

            // Act & Assert - View All Vehicles
            (await custClient.GetAsync("/api/Vehicles")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await drvClient.GetAsync("/api/Vehicles")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await dispClient.GetAsync("/api/Vehicles")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await adminClient.GetAsync("/api/Vehicles")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CustomerMeEndpoint_PermissionMatrix_AllowedForCustomer_ForbiddenForDriver()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"me_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, _) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"me_drv_{Guid.NewGuid()}@test.com");

            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);

            var custClient = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);
            var drvClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            // Act & Assert
            (await custClient.GetAsync("/api/Customers/me")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await drvClient.GetAsync("/api/Customers/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task NotificationOwnership_UserCannotMarkAnotherUsersNotificationAsRead()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (userA, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_own_a_{Guid.NewGuid()}@test.com");
            var (userB, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_own_b_{Guid.NewGuid()}@test.com");

            var notifId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Notifications.Add(new Notification
                {
                    Id = notifId,
                    UserId = userA.Id,
                    Title = "Test Alert for A",
                    Message = "Private Message",
                    Type = NotificationType.DispatchAssignmentReceived,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userB.Id, userB.Email!, userB.UserName!, Roles.Customer);
            var clientB = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenB);

            // Act - User B attempts to mark User A's notification as read
            var patchRes = await clientB.PatchAsync($"/api/Notifications/{notifId}/read", null);

            // Assert
            patchRes.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UnauthenticatedSignalR_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act - attempt negotiate without token
            var notifRes = await client.PostAsync("/hubs/notifications/negotiate", null);
            var trackRes = await client.PostAsync("/hubs/tracking/negotiate", null);

            // Assert
            notifRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            trackRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}

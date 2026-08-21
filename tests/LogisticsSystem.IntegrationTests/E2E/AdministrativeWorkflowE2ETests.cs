using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Drivers;
using LogisticsSystem.Api.Contracts.Roles;
using LogisticsSystem.Api.Contracts.Users;
using LogisticsSystem.Api.Contracts.Vehicles;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.E2E
{
    public class AdministrativeWorkflowE2ETests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AdministrativeWorkflowE2ETests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CompleteAdministrativeWorkflow_EndToEnd()
        {
            // 0. Ensure system roles are seeded
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            // Seed Admin, Dispatcher, Customer
            var adminId = Guid.NewGuid();
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminId, role: Roles.Admin);
            var adminClient = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var dispatcherToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, Guid.NewGuid(), role: Roles.Dispatcher);
            var dispatcherClient = TestAuthHelper.CreateAuthenticatedClient(_factory, dispatcherToken);

            var customerToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, Guid.NewGuid(), role: Roles.Customer);
            var customerClient = TestAuthHelper.CreateAuthenticatedClient(_factory, customerToken);

            var anonymousClient = _factory.CreateClient();

            // 1. Admin creates a new Vehicle
            var plateNumber = $"E2E-{Guid.NewGuid():N}";
            var createVehicleRequest = new CreateVehicleRequest(
                PlateNumber: plateNumber,
                Brand: "Mercedes-Benz",
                Model: "Actros",
                ManufacturingYear: 2023,
                Color: "White",
                Type: VehicleType.Truck,
                Capacity: 25000m);

            var createVehicleResponse = await adminClient.PostAsJsonAsync("/api/Vehicles", createVehicleRequest);
            createVehicleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdVehicle = await createVehicleResponse.Content.ReadFromJsonAsync<VehicleDto>(TestAuthHelper.JsonOptions);
            createdVehicle.Should().NotBeNull();
            createdVehicle!.PlateNumber.Should().Be(plateNumber.ToUpperInvariant());
            createdVehicle.IsActive.Should().BeTrue();

            // 2. Verify Vehicle is Available
            var availableResponse1 = await adminClient.GetAsync("/api/Vehicles/available?pageNumber=1&pageSize=50");
            availableResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
            var availableList1 = await availableResponse1.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>(TestAuthHelper.JsonOptions);
            availableList1.Should().NotBeNull();
            availableList1!.Items.Should().Contain(v => v.Id == createdVehicle.Id);

            // 3. Seed a Driver and Assign the Vehicle
            var (_, driver) = await TestAuthHelper.SeedDriverAsync(
                _factory.Services,
                email: $"e2edriver_{Guid.NewGuid():N}@test.com",
                licenseNumber: $"LIC-{Guid.NewGuid():N}",
                status: DriverStatus.Available);

            var assignRequest = new AssignVehicleRequest(createdVehicle.Id);
            var assignResponse = await adminClient.PostAsJsonAsync($"/api/Drivers/{driver.Id}/vehicle", assignRequest);
            assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 4. Verify Vehicle is now Unavailable (excluded from available list)
            var availableResponse2 = await adminClient.GetAsync("/api/Vehicles/available?pageNumber=1&pageSize=50");
            availableResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
            var availableList2 = await availableResponse2.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>(TestAuthHelper.JsonOptions);
            availableList2.Should().NotBeNull();
            availableList2!.Items.Should().NotContain(v => v.Id == createdVehicle.Id);

            // 5. Verify Driver Dashboard metrics reflect vehicle assignment
            var driverDashboardResponse1 = await adminClient.GetAsync("/api/Dashboard/drivers");
            driverDashboardResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
            var driverMetrics1 = await driverDashboardResponse1.Content.ReadFromJsonAsync<DriverDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            driverMetrics1.Should().NotBeNull();
            driverMetrics1!.DriversWithVehicles.Should().BeGreaterThanOrEqualTo(1);

            // 6. Remove Vehicle from Driver
            var removeResponse = await adminClient.DeleteAsync($"/api/Drivers/{driver.Id}/vehicle");
            removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 7. Verify Vehicle becomes Available again
            var availableResponse3 = await adminClient.GetAsync("/api/Vehicles/available?pageNumber=1&pageSize=50");
            availableResponse3.StatusCode.Should().Be(HttpStatusCode.OK);
            var availableList3 = await availableResponse3.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>(TestAuthHelper.JsonOptions);
            availableList3.Should().NotBeNull();
            availableList3!.Items.Should().Contain(v => v.Id == createdVehicle.Id);

            // 8. Create/Update a User Profile
            var (targetUser, _) = await TestAuthHelper.SeedCustomerAsync(
                _factory.Services,
                email: $"e2e_user_{Guid.NewGuid():N}@test.com");

            var updatedEmail = $"e2e_updated_{Guid.NewGuid():N}@test.com";
            var updateUserRequest = new UpdateUserRequest(
                FirstName: "E2EFirst",
                LastName: "E2ELast",
                PhoneNumber: "+1987654321",
                Email: updatedEmail,
                UserName: $"e2e_usr_{Guid.NewGuid():N}");

            var updateUserResponse = await adminClient.PutAsJsonAsync($"/api/Users/{targetUser.Id}", updateUserRequest);
            updateUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedUserDto = await updateUserResponse.Content.ReadFromJsonAsync<UserDetailsDto>(TestAuthHelper.JsonOptions);
            updatedUserDto.Should().NotBeNull();
            updatedUserDto!.FirstName.Should().Be("E2EFirst");
            updatedUserDto.LastName.Should().Be("E2ELast");
            updatedUserDto.Email.Should().Be(updatedEmail);

            // 9. Admin creates custom role and assigns to user
            var customRoleName = $"FleetSpecialist_{Guid.NewGuid():N}";
            var createRoleResponse = await adminClient.PostAsJsonAsync("/api/Roles", new CreateRoleRequest(customRoleName));
            createRoleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var assignRoleResponse = await adminClient.PostAsJsonAsync(
                $"/api/Roles/users/{targetUser.Id}",
                new AssignRoleRequest(customRoleName));
            assignRoleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify user has the assigned role
            var getUserResponse = await adminClient.GetAsync($"/api/Users/{targetUser.Id}");
            getUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userDetails = await getUserResponse.Content.ReadFromJsonAsync<UserDetailsDto>(TestAuthHelper.JsonOptions);
            userDetails.Should().NotBeNull();
            userDetails!.Roles.Should().Contain(customRoleName);

            // 10. Verify Dashboard Metrics & Recent Activity
            var shipmentMetricsResponse = await adminClient.GetAsync("/api/Dashboard/shipments");
            shipmentMetricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var shipmentMetrics = await shipmentMetricsResponse.Content.ReadFromJsonAsync<ShipmentDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            shipmentMetrics.Should().NotBeNull();

            var recentActivityResponse = await adminClient.GetAsync("/api/Dashboard/recent-activity?pageNumber=1&pageSize=10");
            recentActivityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var recentActivity = await recentActivityResponse.Content.ReadFromJsonAsync<PagedResult<RecentActivityDto>>(TestAuthHelper.JsonOptions);
            recentActivity.Should().NotBeNull();

            // 11. Security & Authorization Validations
            // Anonymous cannot access Users or Dashboard
            var anonUsersResp = await anonymousClient.GetAsync("/api/Users");
            anonUsersResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var anonDashboardResp = await anonymousClient.GetAsync("/api/Dashboard/shipments");
            anonDashboardResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Customer cannot access administration or dashboard
            var custUsersResp = await customerClient.GetAsync("/api/Users");
            custUsersResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var custVehiclesResp = await customerClient.PostAsJsonAsync("/api/Vehicles", createVehicleRequest);
            custVehiclesResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var custDashboardResp = await customerClient.GetAsync("/api/Dashboard/drivers");
            custDashboardResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Dispatcher cannot perform Admin-only operations (Role creation, User modification)
            var dispRoleResp = await dispatcherClient.PostAsJsonAsync("/api/Roles", new CreateRoleRequest("UnauthorizedRole"));
            dispRoleResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var dispUserUpdateResp = await dispatcherClient.PutAsJsonAsync($"/api/Users/{targetUser.Id}", updateUserRequest);
            dispUserUpdateResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}

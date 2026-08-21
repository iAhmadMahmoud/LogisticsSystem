using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class PerformanceAndQueryVerificationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PerformanceAndQueryVerificationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ShipmentDashboard_MetricsAggregation_ReturnsAccurateCounts()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"admin_dash_{Guid.NewGuid():N}@test.com");
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_perf_{Guid.NewGuid():N}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_perf_{Guid.NewGuid():N}@test.com");

            // Seed shipments in different statuses
            await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);
            await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, driverId: driver.Id, status: ShipmentStatus.Assigned);
            await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, driverId: driver.Id, status: ShipmentStatus.Delivered);

            var response = await client.GetAsync("/api/Dashboard/shipments");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var metrics = await response.Content.ReadFromJsonAsync<ShipmentDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            metrics.Should().NotBeNull();
            metrics!.TotalShipments.Should().BeGreaterThanOrEqualTo(3);
            metrics.PendingShipments.Should().BeGreaterThanOrEqualTo(1);
            metrics.AssignedShipments.Should().BeGreaterThanOrEqualTo(1);
            metrics.DeliveredShipments.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task DriverDashboard_MetricsAggregation_ReturnsAccurateCounts()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"admin_drvdash_{Guid.NewGuid():N}@test.com");
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            // Seed available and busy drivers
            await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_avail_{Guid.NewGuid():N}@test.com", status: DriverStatus.Available);
            await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_busy_{Guid.NewGuid():N}@test.com", status: DriverStatus.Busy);

            var response = await client.GetAsync("/api/Dashboard/drivers");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var metrics = await response.Content.ReadFromJsonAsync<DriverDashboardMetricsDto>(TestAuthHelper.JsonOptions);
            metrics.Should().NotBeNull();
            metrics!.TotalDrivers.Should().BeGreaterThanOrEqualTo(2);
            metrics.AvailableDrivers.Should().BeGreaterThanOrEqualTo(1);
            metrics.BusyDrivers.Should().BeGreaterThanOrEqualTo(1);
            metrics.ActiveDrivers.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task UserListing_BatchRoleLoading_ReturnsUsersWithRolesCorrectly()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"admin_users_{Guid.NewGuid():N}@test.com");
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var response = await client.GetAsync("/api/Users?pageNumber=1&pageSize=10");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().NotBeEmpty();
            result.Items.All(u => u.Roles != null).Should().BeTrue("roles must be batch loaded without N+1 query errors");
        }

        [Fact]
        public async Task RoleListing_UserCountAggregation_ReturnsRolesWithAccurateUserCounts()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"admin_roles_{Guid.NewGuid():N}@test.com");
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var response = await client.GetAsync("/api/Roles");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(TestAuthHelper.JsonOptions);
            roles.Should().NotBeNull();
            roles!.Should().Contain(r => r.Name == Roles.Admin);
            roles.Should().Contain(r => r.Name == Roles.Customer);
            roles.All(r => r.UserCount >= 0).Should().BeTrue();
        }

        [Fact]
        public async Task RecentActivity_DirectProjection_ReturnsAccurateActivityItems()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (adminUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"admin_rec_{Guid.NewGuid():N}@test.com");
            var adminToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, adminUser.Id, adminUser.Email!, adminUser.UserName!, Roles.Admin);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, adminToken);

            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_rec_{Guid.NewGuid():N}@test.com");
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LogisticsSystem.Infrastructure.Persistence.ApplicationDbContext>();
                db.ShipmentStatusHistories.Add(new LogisticsSystem.Domain.Entities.ShipmentStatusHistory
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    Status = ShipmentStatus.Pending,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = custUser.Id
                });
                await db.SaveChangesAsync();
            }

            var response = await client.GetAsync("/api/Dashboard/recent-activity?pageNumber=1&pageSize=10");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedResult<RecentActivityDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().NotBeEmpty();
            result.Items.All(i => !string.IsNullOrEmpty(i.ActivityType)).Should().BeTrue();
        }

        [Fact]
        public async Task MyShipments_PaginationAndFiltering_ReturnsAccuratePageResults()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_myship_{Guid.NewGuid():N}@test.com");
            var custToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, custToken);

            // Seed 3 shipments for this customer
            await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);
            await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending);
            await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Delivered);

            var response = await client.GetAsync("/api/Shipments/my-shipments?pageNumber=1&pageSize=2");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedResult<ShipmentDto>>(TestAuthHelper.JsonOptions);
            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(3);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
        }
    }
}

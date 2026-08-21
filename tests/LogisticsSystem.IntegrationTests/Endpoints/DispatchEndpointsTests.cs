using System.Net;
using System.Text.Json;
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
    public class DispatchEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DispatchEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AcceptAssignment_WhenValid_UpdatesAssignmentAndShipmentAndDriverStatusInDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"dispatch_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"dispatch_drv_{Guid.NewGuid()}@test.com", status: DriverStatus.Available);

            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, status: ShipmentStatus.Pending, trackingNumber: $"TRK-DISP-{Guid.NewGuid():N}"[..12]);

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
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify Database Persistence
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var updatedAssignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
                updatedAssignment.Should().NotBeNull();
                updatedAssignment!.Status.Should().Be(AssignmentStatus.Accepted);
                updatedAssignment.RespondedAt.Should().NotBeNull();

                var updatedShipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == shipment.Id);
                updatedShipment.Should().NotBeNull();
                updatedShipment!.Status.Should().Be(ShipmentStatus.Assigned);
                updatedShipment.DriverId.Should().Be(driver.Id);

                var updatedDriver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
                updatedDriver.Should().NotBeNull();
                updatedDriver!.Status.Should().Be(DriverStatus.Busy);
            }
        }

        [Fact]
        public async Task RejectAssignment_WhenValid_UpdatesAssignmentStatusToRejectedInDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"disp_rej_cust_{Guid.NewGuid()}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"disp_rej_drv_{Guid.NewGuid()}@test.com");

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
            var response = await client.PostAsync($"/api/Dispatch/assignments/{assignmentId}/reject", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var updatedAssignment = await db.DispatchAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
                updatedAssignment.Should().NotBeNull();
                updatedAssignment!.Status.Should().Be(AssignmentStatus.Rejected);
                updatedAssignment.RespondedAt.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GetMyAssignments_WhenCalledByCustomer_ReturnsForbidden()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (custUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_no_disp_{Guid.NewGuid()}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, custUser.Id, custUser.Email!, custUser.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.GetAsync("/api/Dispatch/my-assignments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}

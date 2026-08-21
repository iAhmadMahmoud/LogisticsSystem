using FluentAssertions;
using LogisticsSystem.Application.Features.Dashboard.Queries.GetDriverDashboardMetrics;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsSystem.UnitTests.Dashboard
{
    public class GetDriverDashboardMetricsQueryHandlerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GetDriverDashboardMetricsQueryHandler _handler;

        public GetDriverDashboardMetricsQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"DriverDashboardTests_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _handler = new GetDriverDashboardMetricsQueryHandler(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Handle_WhenNoDriversExist_ReturnsAllZeroMetrics()
        {
            // Act
            var result = await _handler.Handle(new GetDriverDashboardMetricsQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalDrivers.Should().Be(0);
            result.AvailableDrivers.Should().Be(0);
            result.BusyDrivers.Should().Be(0);
            result.OfflineDrivers.Should().Be(0);
            result.OnBreakDrivers.Should().Be(0);
            result.SuspendedDrivers.Should().Be(0);
            result.DriversWithVehicles.Should().Be(0);
            result.DriversWithoutVehicles.Should().Be(0);
            result.ActiveDrivers.Should().Be(0);
            result.InactiveDrivers.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenDriversExist_AggregatesCountsCorrectly()
        {
            // Arrange
            var vehicleId1 = Guid.NewGuid();
            var vehicleId2 = Guid.NewGuid();

            var drivers = new List<Driver>
            {
                new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    LicenseNumber = "LIC-001",
                    Status = DriverStatus.Available,
                    VehicleId = vehicleId1
                },
                new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    LicenseNumber = "LIC-002",
                    Status = DriverStatus.Available,
                    VehicleId = null
                },
                new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    LicenseNumber = "LIC-003",
                    Status = DriverStatus.Busy,
                    VehicleId = vehicleId2
                },
                new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    LicenseNumber = "LIC-004",
                    Status = DriverStatus.Offline,
                    VehicleId = null
                },
                new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    LicenseNumber = "LIC-005",
                    Status = DriverStatus.OnBreak,
                    VehicleId = null
                },
                new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    LicenseNumber = "LIC-006",
                    Status = DriverStatus.Suspended,
                    VehicleId = null
                }
            };

            await _context.Drivers.AddRangeAsync(drivers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetDriverDashboardMetricsQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalDrivers.Should().Be(6);
            result.AvailableDrivers.Should().Be(2);
            result.BusyDrivers.Should().Be(1);
            result.OfflineDrivers.Should().Be(1);
            result.OnBreakDrivers.Should().Be(1);
            result.SuspendedDrivers.Should().Be(1);
            result.DriversWithVehicles.Should().Be(2);
            result.DriversWithoutVehicles.Should().Be(4);
            result.ActiveDrivers.Should().Be(5); // 6 total - 1 suspended
            result.InactiveDrivers.Should().Be(1); // 1 suspended
        }

        [Fact]
        public async Task Handle_WhenAllDriversSuspended_ActiveDriversIsZeroAndInactiveEqualsTotal()
        {
            // Arrange
            var drivers = new List<Driver>
            {
                new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), LicenseNumber = "S1", Status = DriverStatus.Suspended },
                new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), LicenseNumber = "S2", Status = DriverStatus.Suspended }
            };

            await _context.Drivers.AddRangeAsync(drivers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetDriverDashboardMetricsQuery(), CancellationToken.None);

            // Assert
            result.TotalDrivers.Should().Be(2);
            result.SuspendedDrivers.Should().Be(2);
            result.ActiveDrivers.Should().Be(0);
            result.InactiveDrivers.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenAllDriversAssignedVehicles_DriversWithoutVehiclesIsZero()
        {
            // Arrange
            var drivers = new List<Driver>
            {
                new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), LicenseNumber = "V1", Status = DriverStatus.Available, VehicleId = Guid.NewGuid() },
                new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), LicenseNumber = "V2", Status = DriverStatus.Busy, VehicleId = Guid.NewGuid() }
            };

            await _context.Drivers.AddRangeAsync(drivers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetDriverDashboardMetricsQuery(), CancellationToken.None);

            // Assert
            result.TotalDrivers.Should().Be(2);
            result.DriversWithVehicles.Should().Be(2);
            result.DriversWithoutVehicles.Should().Be(0);
        }
    }
}

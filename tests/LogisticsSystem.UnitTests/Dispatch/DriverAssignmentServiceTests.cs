using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Dispatch
{
    public class DriverAssignmentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock;
        private readonly Mock<IGenericRepository<DispatchAssignment>> _assignmentRepoMock;
        private readonly DriverAssignmentService _service;

        public DriverAssignmentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"DriverAssignmentTests_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);

            _driverRepoMock = new Mock<IGenericRepository<Driver>>();
            _assignmentRepoMock = new Mock<IGenericRepository<DispatchAssignment>>();

            _assignmentRepoMock.Setup(r => r.AsQueryable()).Returns(_context.DispatchAssignments);

            _service = new DriverAssignmentService(
                _driverRepoMock.Object,
                _assignmentRepoMock.Object,
                new Mock<ILogger<DriverAssignmentService>>().Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task FindBestAvailableDriverAsync_WhenDriversAvailable_ReturnsNearestEligibleDriver()
        {
            // Arrange
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                PickupLatitude = 30.0444,
                PickupLongitude = 31.2357 // Cairo Center
            };

            var farDriver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LicenseNumber = "DL-FAR",
                Status = DriverStatus.Available,
                Latitude = 31.2001,
                Longitude = 29.9187 // Alexandria (~180km away)
            };

            var nearDriver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LicenseNumber = "DL-NEAR",
                Status = DriverStatus.Available,
                Latitude = 30.0500,
                Longitude = 31.2400 // ~1km away
            };

            _driverRepoMock
                .Setup(r => r.ListAsync(It.IsAny<ISpecification<Driver>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Driver> { farDriver, nearDriver });

            // Act
            var bestDriver = await _service.FindBestAvailableDriverAsync(shipment, CancellationToken.None);

            // Assert
            bestDriver.Should().NotBeNull();
            bestDriver!.Id.Should().Be(nearDriver.Id);
        }

        [Fact]
        public async Task FindBestAvailableDriverAsync_ExcludesDriversWhoPreviouslyRejectedShipment()
        {
            // Arrange
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                PickupLatitude = 30.0444,
                PickupLongitude = 31.2357
            };

            var rejectedNearDriver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LicenseNumber = "DL-REJECTED",
                Status = DriverStatus.Available,
                Latitude = 30.0445,
                Longitude = 31.2358 // Very close
            };

            var nextAvailableDriver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LicenseNumber = "DL-NEXT",
                Status = DriverStatus.Available,
                Latitude = 30.0600,
                Longitude = 31.2500
            };

            await _context.DispatchAssignments.AddAsync(new DispatchAssignment
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                DriverId = rejectedNearDriver.Id,
                AttemptNumber = 1,
                Status = AssignmentStatus.Rejected,
                SentAt = DateTime.UtcNow.AddMinutes(-10)
            });
            await _context.SaveChangesAsync();

            _driverRepoMock
                .Setup(r => r.ListAsync(It.IsAny<ISpecification<Driver>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Driver> { rejectedNearDriver, nextAvailableDriver });

            // Act
            var bestDriver = await _service.FindBestAvailableDriverAsync(shipment, CancellationToken.None);

            // Assert
            bestDriver.Should().NotBeNull();
            bestDriver!.Id.Should().Be(nextAvailableDriver.Id);
        }

        [Fact]
        public async Task FindBestAvailableDriverAsync_WhenDriversHaveNoLocation_ReturnsNull()
        {
            // Arrange
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                PickupLatitude = 30.0444,
                PickupLongitude = 31.2357
            };

            var noLocDriver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LicenseNumber = "DL-NOLOC",
                Status = DriverStatus.Available,
                Latitude = null,
                Longitude = null
            };

            _driverRepoMock
                .Setup(r => r.ListAsync(It.IsAny<ISpecification<Driver>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Driver> { noLocDriver });

            // Act
            var bestDriver = await _service.FindBestAvailableDriverAsync(shipment, CancellationToken.None);

            // Assert
            bestDriver.Should().BeNull();
        }
    }
}

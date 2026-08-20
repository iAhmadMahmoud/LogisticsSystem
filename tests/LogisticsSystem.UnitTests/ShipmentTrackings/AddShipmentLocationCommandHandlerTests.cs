using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Application.Features.ShipmentTrackings.Commands.AddShipmentLocation;
using LogisticsSystem.Application.Features.ShipmentTrackings.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.ShipmentTrackings
{
    public class AddShipmentLocationCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock = new();
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock = new();
        private readonly Mock<IGenericRepository<ShipmentTracking>> _trackingRepoMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ITrackingRealtimeService> _trackingRealtimeServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private readonly AddShipmentLocationCommandHandler _handler;

        public AddShipmentLocationCommandHandlerTests()
        {
            _handler = new AddShipmentLocationCommandHandler(
                _shipmentRepoMock.Object,
                _driverRepoMock.Object,
                _trackingRepoMock.Object,
                _currentUserServiceMock.Object,
                _trackingRealtimeServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesDriverLocation_SavesTracking_AndBroadcastsLocation()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var driver = new Driver
            {
                Id = driverId,
                UserId = driverUserId,
                Latitude = 30.0,
                Longitude = 31.0
            };

            var shipment = new Shipment
            {
                Id = shipmentId,
                DriverId = driverId,
                Status = ShipmentStatus.InTransit
            };

            var command = new AddShipmentLocationCommand(shipmentId, 30.05, 31.05);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);
            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<DriverByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);
            _trackingRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<LatestShipmentTrackingSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShipmentTracking?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            driver.Latitude.Should().Be(30.05);
            driver.Longitude.Should().Be(31.05);
            _driverRepoMock.Verify(x => x.Update(driver), Times.Once);

            _trackingRepoMock.Verify(x => x.AddAsync(
                It.Is<ShipmentTracking>(t => t.ShipmentId == shipmentId && t.Latitude == 30.05 && t.Longitude == 31.05),
                It.IsAny<CancellationToken>()), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _trackingRealtimeServiceMock.Verify(x => x.LocationUpdatedAsync(
                shipmentId,
                driverId,
                30.05,
                31.05,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShipmentNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Shipment?)null);

            var command = new AddShipmentLocationCommand(shipmentId, 30.05, 31.05);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Shipment not found.");
        }

        [Fact]
        public async Task Handle_DriverNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var shipment = new Shipment { Id = shipmentId, DriverId = Guid.NewGuid(), Status = ShipmentStatus.InTransit };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);
            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<DriverByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null);

            var command = new AddShipmentLocationCommand(shipmentId, 30.05, 31.05);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Driver profile not found.");
        }

        [Fact]
        public async Task Handle_DriverNotAssignedToShipment_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId };
            var shipment = new Shipment { Id = shipmentId, DriverId = Guid.NewGuid(), Status = ShipmentStatus.InTransit };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);
            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<DriverByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            var command = new AddShipmentLocationCommand(shipmentId, 30.05, 31.05);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not assigned to this shipment.");
        }

        [Fact]
        public async Task Handle_ShipmentNotInTransit_ThrowsDomainException()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId };
            var shipment = new Shipment { Id = shipmentId, DriverId = driverId, Status = ShipmentStatus.Pending };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);
            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<DriverByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            var command = new AddShipmentLocationCommand(shipmentId, 30.05, 31.05);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Location tracking is only allowed when the shipment is in transit.");
        }

        [Fact]
        public async Task Handle_SameLocationAsLatest_ThrowsDomainException()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId };
            var shipment = new Shipment { Id = shipmentId, DriverId = driverId, Status = ShipmentStatus.InTransit };
            var latestTracking = new ShipmentTracking { ShipmentId = shipmentId, Latitude = 30.05, Longitude = 31.05 };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);
            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<DriverByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);
            _trackingRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<LatestShipmentTrackingSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(latestTracking);

            var command = new AddShipmentLocationCommand(shipmentId, 30.05, 31.05);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("The submitted location is the same as the latest recorded location.");
        }
    }
}

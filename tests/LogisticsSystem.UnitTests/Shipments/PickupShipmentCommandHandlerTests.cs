using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Commands.PickupShipment;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Shipments
{
    public class PickupShipmentCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock = new();
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock = new();
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<IShipmentStatusHistoryService> _statusHistoryServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<ITrackingRealtimeService> _trackingRealtimeServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private readonly PickupShipmentCommandHandler _handler;

        public PickupShipmentCommandHandlerTests()
        {
            _handler = new PickupShipmentCommandHandler(
                _shipmentRepoMock.Object,
                _unitOfWorkMock.Object,
                _statusHistoryServiceMock.Object,
                _currentUserServiceMock.Object,
                _driverRepoMock.Object,
                _notificationServiceMock.Object,
                _customerRepoMock.Object,
                _trackingRealtimeServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesStatusToPickedUp_AndBroadcastsStatus()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var customerUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId };
            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                DriverId = driverId,
                Status = ShipmentStatus.Assigned,
                TrackingNumber = "TRK-PICKUP-01"
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);
            _customerRepoMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<DriverByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            var command = new PickupShipmentCommand(shipmentId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            shipment.Status.Should().Be(ShipmentStatus.PickedUp);
            shipment.PickedUpAt.Should().NotBeNull();
            _shipmentRepoMock.Verify(x => x.Update(shipment), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _trackingRealtimeServiceMock.Verify(x => x.ShipmentStatusChangedAsync(
                shipmentId,
                ShipmentStatus.PickedUp,
                It.IsAny<DateTime>(),
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidTransition_ThrowsDomainException()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var customerUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId };
            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                DriverId = driverId,
                Status = ShipmentStatus.Delivered,
                TrackingNumber = "TRK-PICKUP-02"
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);
            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);
            _customerRepoMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<DriverByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            var command = new PickupShipmentCommand(shipmentId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Shipment cannot transition from Delivered to PickedUp.");
        }
    }
}

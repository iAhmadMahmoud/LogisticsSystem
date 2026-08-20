using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Shipments
{
    public class CancelShipmentCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock = new();
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock = new();
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<IShipmentStatusHistoryService> _statusHistoryServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<ITrackingRealtimeService> _trackingRealtimeServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private readonly CancelShipmentCommandHandler _handler;

        public CancelShipmentCommandHandlerTests()
        {
            _handler = new CancelShipmentCommandHandler(
                _shipmentRepoMock.Object,
                _currentUserServiceMock.Object,
                _statusHistoryServiceMock.Object,
                _driverRepoMock.Object,
                _unitOfWorkMock.Object,
                _customerRepoMock.Object,
                _notificationServiceMock.Object,
                _trackingRealtimeServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenShipmentIsPending_CancelsShipmentAndNotifiesCustomer()
        {
            // Arrange
            var customerUserId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                Status = ShipmentStatus.Pending,
                TrackingNumber = "TRK-100"
            };

            _currentUserServiceMock.Setup(x => x.IsInRole(Roles.Customer)).Returns(true);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(customerUserId);

            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<CustomerByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _customerRepoMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            await _handler.Handle(new CancelShipmentCommand(shipmentId), CancellationToken.None);

            // Assert
            shipment.Status.Should().Be(ShipmentStatus.Cancelled);
            shipment.CancelledAt.Should().NotBeNull();

            _shipmentRepoMock.Verify(x => x.Update(shipment), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _notificationServiceMock.Verify(x => x.CreateAsync(
                customerUserId,
                "Shipment Cancelled",
                It.IsAny<string>(),
                NotificationType.ShipmentCancelled,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenShipmentIsAssigned_CancelsShipment_RestoresDriverToAvailable_AndNotifiesBoth()
        {
            // Arrange
            var customerUserId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var driver = new Driver { Id = driverId, UserId = driverUserId, Status = DriverStatus.Busy };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                DriverId = driverId,
                Status = ShipmentStatus.Assigned,
                TrackingNumber = "TRK-200"
            };

            _currentUserServiceMock.Setup(x => x.IsInRole(Roles.Customer)).Returns(true);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(customerUserId);

            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<CustomerByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _customerRepoMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _driverRepoMock.Setup(x => x.GetByIdAsync(driverId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            // Act
            await _handler.Handle(new CancelShipmentCommand(shipmentId), CancellationToken.None);

            // Assert
            shipment.Status.Should().Be(ShipmentStatus.Cancelled);
            driver.Status.Should().Be(DriverStatus.Available);

            _driverRepoMock.Verify(x => x.Update(driver), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Both customer and driver notified
            _notificationServiceMock.Verify(x => x.CreateAsync(
                customerUserId,
                "Shipment Cancelled",
                It.IsAny<string>(),
                NotificationType.ShipmentCancelled,
                It.IsAny<CancellationToken>()), Times.Once);

            _notificationServiceMock.Verify(x => x.CreateAsync(
                driverUserId,
                "Shipment Cancelled",
                It.IsAny<string>(),
                NotificationType.ShipmentCancelled,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenShipmentIsDelivered_ThrowsDomainException()
        {
            // Arrange
            var customerUserId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                Status = ShipmentStatus.Delivered,
                TrackingNumber = "TRK-300"
            };

            _currentUserServiceMock.Setup(x => x.IsInRole(Roles.Customer)).Returns(true);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(customerUserId);

            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<CustomerByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _customerRepoMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            var act = async () => await _handler.Handle(new CancelShipmentCommand(shipmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*cannot transition from Delivered to Cancelled*");
        }

        [Fact]
        public async Task Handle_WhenCustomerDoesNotOwnShipment_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var customerUserId = Guid.NewGuid();
            var actualOwnerCustomerId = Guid.NewGuid();
            var otherCustomerId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var loggedInCustomer = new Customer { Id = otherCustomerId, UserId = customerUserId };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = actualOwnerCustomerId,
                Status = ShipmentStatus.Pending,
                TrackingNumber = "TRK-400"
            };

            _currentUserServiceMock.Setup(x => x.IsInRole(Roles.Customer)).Returns(true);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(customerUserId);

            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<CustomerByUserIdSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(loggedInCustomer);

            // Act
            var act = async () => await _handler.Handle(new CancelShipmentCommand(shipmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not allowed to cancel this shipment.");
        }
    }
}

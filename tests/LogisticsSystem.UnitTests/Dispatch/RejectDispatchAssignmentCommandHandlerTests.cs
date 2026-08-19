using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Dispatch.Commands.RejectDispatchAssignment;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using LogisticsSystem.UnitTests.Helpers;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Dispatch
{
    public class RejectDispatchAssignmentCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<DispatchAssignment>> _dispatchAssignmentRepoMock = new();
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock = new();
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock = new();
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IDriverAssignmentService> _driverAssignmentServiceMock = new();
        private readonly Mock<IDispatchAssignmentService> _dispatchAssignmentServiceMock = new();

        private readonly RejectDispatchAssignmentCommandHandler _handler;

        public RejectDispatchAssignmentCommandHandlerTests()
        {
            _handler = new RejectDispatchAssignmentCommandHandler(
                _dispatchAssignmentRepoMock.Object,
                _driverRepoMock.Object,
                _shipmentRepoMock.Object,
                _customerRepoMock.Object,
                _notificationServiceMock.Object,
                _currentUserServiceMock.Object,
                _unitOfWorkMock.Object,
                _driverAssignmentServiceMock.Object,
                _dispatchAssignmentServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenNextDriverAvailable_RejectsAndReassigns()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var nextDriverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId };
            var nextDriver = new Driver { Id = nextDriverId, UserId = Guid.NewGuid(), Status = DriverStatus.Available };
            var shipment = new Shipment { Id = shipmentId, CustomerId = Guid.NewGuid(), Status = ShipmentStatus.Pending };
            var assignment = new DispatchAssignment
            {
                Id = assignmentId,
                DriverId = driverId,
                ShipmentId = shipmentId,
                Status = AssignmentStatus.Pending
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);

            _dispatchAssignmentRepoMock.Setup(x => x.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            var drivers = new List<Driver> { driver }.AsAsyncQueryable();
            _driverRepoMock.Setup(x => x.AsQueryable()).Returns(drivers);

            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _driverAssignmentServiceMock.Setup(x => x.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync(nextDriver);

            // Act
            await _handler.Handle(new RejectDispatchAssignmentCommand(assignmentId), CancellationToken.None);

            // Assert
            assignment.Status.Should().Be(AssignmentStatus.Rejected);
            _dispatchAssignmentRepoMock.Verify(x => x.Update(assignment), Times.Once);
            _dispatchAssignmentServiceMock.Verify(x => x.CreateAssignmentAsync(shipment, nextDriver, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenNoNextDriverAvailable_SendsNoDriverAvailableNotificationToCustomer()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var customerUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId };
            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                Status = ShipmentStatus.Pending,
                TrackingNumber = "TRK-NODRIVER-1"
            };
            var assignment = new DispatchAssignment
            {
                Id = assignmentId,
                DriverId = driverId,
                ShipmentId = shipmentId,
                Status = AssignmentStatus.Pending
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);

            _dispatchAssignmentRepoMock.Setup(x => x.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            var drivers = new List<Driver> { driver }.AsAsyncQueryable();
            _driverRepoMock.Setup(x => x.AsQueryable()).Returns(drivers);

            _shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _customerRepoMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _driverAssignmentServiceMock.Setup(x => x.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null);

            // Act
            await _handler.Handle(new RejectDispatchAssignmentCommand(assignmentId), CancellationToken.None);

            // Assert
            assignment.Status.Should().Be(AssignmentStatus.Rejected);
            _dispatchAssignmentServiceMock.Verify(x => x.CreateAssignmentAsync(It.IsAny<Shipment>(), It.IsAny<Driver>(), It.IsAny<CancellationToken>()), Times.Never);

            _notificationServiceMock.Verify(x => x.CreateAsync(
                customerUserId,
                "No Driver Available",
                It.IsAny<string>(),
                NotificationType.NoDriverAvailable,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

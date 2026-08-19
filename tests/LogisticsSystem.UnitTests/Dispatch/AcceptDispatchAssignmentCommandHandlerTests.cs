using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using LogisticsSystem.UnitTests.Helpers;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Dispatch
{
    public class AcceptDispatchAssignmentCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<DispatchAssignment>> _dispatchAssignmentRepoMock = new();
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock = new();
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock = new();
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<IShipmentStatusHistoryService> _statusHistoryServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private readonly AcceptDispatchAssignmentCommandHandler _handler;

        public AcceptDispatchAssignmentCommandHandlerTests()
        {
            _handler = new AcceptDispatchAssignmentCommandHandler(
                _dispatchAssignmentRepoMock.Object,
                _driverRepoMock.Object,
                _shipmentRepoMock.Object,
                _customerRepoMock.Object,
                _statusHistoryServiceMock.Object,
                _currentUserServiceMock.Object,
                _notificationServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenValid_AcceptsAssignment_MarksDriverBusy_AndNotifiesCustomer()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var customerUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId, Status = DriverStatus.Available };
            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                Status = ShipmentStatus.Pending,
                TrackingNumber = "TRK-ASSIGN-100"
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

            // Act
            await _handler.Handle(new AcceptDispatchAssignmentCommand(assignmentId), CancellationToken.None);

            // Assert
            assignment.Status.Should().Be(AssignmentStatus.Accepted);
            shipment.Status.Should().Be(ShipmentStatus.Assigned);
            shipment.DriverId.Should().Be(driverId);
            driver.Status.Should().Be(DriverStatus.Busy);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _notificationServiceMock.Verify(x => x.CreateAsync(
                customerUserId,
                "Shipment Assigned",
                It.IsAny<string>(),
                NotificationType.ShipmentAssigned,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenDriverNotAvailable_ThrowsDomainException()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId, Status = DriverStatus.Busy };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = Guid.NewGuid(),
                Status = ShipmentStatus.Pending
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

            // Act
            var act = async () => await _handler.Handle(new AcceptDispatchAssignmentCommand(assignmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Driver is no longer available.");
        }
    }
}

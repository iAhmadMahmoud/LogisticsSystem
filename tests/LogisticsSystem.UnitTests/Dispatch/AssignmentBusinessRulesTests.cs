using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment;
using LogisticsSystem.Application.Features.Dispatch.Commands.RejectDispatchAssignment;
using LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using LogisticsSystem.Infrastructure.Services;
using LogisticsSystem.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Dispatch
{
    public class AssignmentBusinessRulesTests
    {
        [Fact]
        public async Task AvailableDriver_CanBeAssigned_IncrementsAttemptCounterAndSendsNotification()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LogisticsSystem.Infrastructure.Persistence.ApplicationDbContext>()
                .UseInMemoryDatabase($"AssignRules_{Guid.NewGuid()}")
                .Options;

            using var context = new LogisticsSystem.Infrastructure.Persistence.ApplicationDbContext(options);

            var assignmentRepoMock = new Mock<IGenericRepository<DispatchAssignment>>();
            assignmentRepoMock.Setup(r => r.AsQueryable()).Returns(context.DispatchAssignments);

            var notifServiceMock = new Mock<INotificationService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var dispatchService = new DispatchAssignmentService(
                assignmentRepoMock.Object,
                unitOfWorkMock.Object,
                notifServiceMock.Object);

            var shipment = new Shipment { Id = Guid.NewGuid(), TrackingNumber = "TRK-RULE-1" };
            var driver = new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = DriverStatus.Available };

            // Act
            var assignment = await dispatchService.CreateAssignmentAsync(shipment, driver, CancellationToken.None);

            // Assert
            assignment.Should().NotBeNull();
            assignment!.Status.Should().Be(AssignmentStatus.Pending);
            assignment.AttemptNumber.Should().Be(1);
            assignment.DriverId.Should().Be(driver.Id);
            assignment.ShipmentId.Should().Be(shipment.Id);

            assignmentRepoMock.Verify(r => r.AddAsync(It.Is<DispatchAssignment>(a => a.AttemptNumber == 1 && a.Status == AssignmentStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);
            notifServiceMock.Verify(n => n.CreateAsync(
                driver.UserId,
                "New Shipment Assignment",
                It.Is<string>(m => m.Contains(shipment.TrackingNumber)),
                NotificationType.DispatchAssignmentReceived,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UnavailableDriver_BusyOrOffline_ThrowsDomainExceptionOnAcceptance()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId, Status = DriverStatus.Offline };
            var shipment = new Shipment { Id = shipmentId, CustomerId = Guid.NewGuid(), Status = ShipmentStatus.Pending };
            var assignment = new DispatchAssignment { Id = assignmentId, DriverId = driverId, ShipmentId = shipmentId, Status = AssignmentStatus.Pending };

            var dispatchAssignmentRepoMock = new Mock<IGenericRepository<DispatchAssignment>>();
            dispatchAssignmentRepoMock.Setup(x => x.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

            var driverRepoMock = new Mock<IGenericRepository<Driver>>();
            var drivers = new List<Driver> { driver }.AsAsyncQueryable();
            driverRepoMock.Setup(x => x.AsQueryable()).Returns(drivers);

            var shipmentRepoMock = new Mock<IGenericRepository<Shipment>>();
            shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

            var customerRepoMock = new Mock<IGenericRepository<Customer>>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);

            var handler = new AcceptDispatchAssignmentCommandHandler(
                dispatchAssignmentRepoMock.Object,
                driverRepoMock.Object,
                shipmentRepoMock.Object,
                customerRepoMock.Object,
                Mock.Of<IShipmentStatusHistoryService>(),
                currentUserServiceMock.Object,
                Mock.Of<INotificationService>(),
                Mock.Of<ITrackingRealtimeService>(),
                Mock.Of<IUnitOfWork>());

            // Act
            var act = async () => await handler.Handle(new AcceptDispatchAssignmentCommand(assignmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Driver is no longer available.");
        }

        [Fact]
        public async Task ConflictingAssignment_ShipmentAlreadyHasDriver_ThrowsDomainException()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var existingDriverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId, Status = DriverStatus.Available };
            var shipment = new Shipment { Id = shipmentId, CustomerId = Guid.NewGuid(), DriverId = existingDriverId, Status = ShipmentStatus.Assigned };
            var assignment = new DispatchAssignment { Id = assignmentId, DriverId = driverId, ShipmentId = shipmentId, Status = AssignmentStatus.Pending };

            var dispatchAssignmentRepoMock = new Mock<IGenericRepository<DispatchAssignment>>();
            dispatchAssignmentRepoMock.Setup(x => x.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

            var driverRepoMock = new Mock<IGenericRepository<Driver>>();
            driverRepoMock.Setup(x => x.AsQueryable()).Returns(new List<Driver> { driver }.AsAsyncQueryable());

            var shipmentRepoMock = new Mock<IGenericRepository<Shipment>>();
            shipmentRepoMock.Setup(x => x.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns(driverUserId);

            var handler = new AcceptDispatchAssignmentCommandHandler(
                dispatchAssignmentRepoMock.Object,
                driverRepoMock.Object,
                shipmentRepoMock.Object,
                Mock.Of<IGenericRepository<Customer>>(),
                Mock.Of<IShipmentStatusHistoryService>(),
                currentUserServiceMock.Object,
                Mock.Of<INotificationService>(),
                Mock.Of<ITrackingRealtimeService>(),
                Mock.Of<IUnitOfWork>());

            // Act
            var act = async () => await handler.Handle(new AcceptDispatchAssignmentCommand(assignmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>();
        }

        [Fact]
        public async Task AssignmentCancellation_ReleasesAssignedDriverToAvailable_AndEmitsSignalREvents()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var customerUserId = Guid.NewGuid();
            var driverUserId = Guid.NewGuid();

            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var driver = new Driver { Id = driverId, UserId = driverUserId, Status = DriverStatus.Busy };
            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                DriverId = driverId,
                Status = ShipmentStatus.Assigned,
                TrackingNumber = "TRK-CANCEL-1"
            };

            var shipmentRepoMock = new Mock<IGenericRepository<Shipment>>();
            shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

            var driverRepoMock = new Mock<IGenericRepository<Driver>>();
            driverRepoMock.Setup(r => r.GetByIdAsync(driverId, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

            var customerRepoMock = new Mock<IGenericRepository<Customer>>();
            customerRepoMock.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
            currentUserServiceMock.Setup(u => u.IsInRole(LogisticsSystem.Domain.Constants.Roles.Customer)).Returns(false);

            var notifServiceMock = new Mock<INotificationService>();
            var trackingRealtimeMock = new Mock<ITrackingRealtimeService>();
            var statusHistoryMock = new Mock<IShipmentStatusHistoryService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var handler = new CancelShipmentCommandHandler(
                shipmentRepoMock.Object,
                currentUserServiceMock.Object,
                statusHistoryMock.Object,
                driverRepoMock.Object,
                unitOfWorkMock.Object,
                customerRepoMock.Object,
                notifServiceMock.Object,
                trackingRealtimeMock.Object);

            // Act
            await handler.Handle(new CancelShipmentCommand(shipmentId), CancellationToken.None);

            // Assert
            shipment.Status.Should().Be(ShipmentStatus.Cancelled);
            shipment.CancelledAt.Should().NotBeNull();
            driver.Status.Should().Be(DriverStatus.Available);

            driverRepoMock.Verify(r => r.Update(It.Is<Driver>(d => d.Status == DriverStatus.Available)), Times.Once);
            statusHistoryMock.Verify(s => s.AddAsync(shipment, ShipmentStatus.Cancelled, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
            notifServiceMock.Verify(n => n.CreateAsync(customerUserId, "Shipment Cancelled", It.IsAny<string>(), NotificationType.ShipmentCancelled, It.IsAny<CancellationToken>()), Times.Once);
            notifServiceMock.Verify(n => n.CreateAsync(driverUserId, "Shipment Cancelled", It.IsAny<string>(), NotificationType.ShipmentCancelled, It.IsAny<CancellationToken>()), Times.Once);
            trackingRealtimeMock.Verify(t => t.ShipmentStatusChangedAsync(shipmentId, ShipmentStatus.Cancelled, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AssignmentRejection_WhenNoAlternativeDriverAvailable_NotifiesCustomerWithNoDriverAvailable()
        {
            // Arrange
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var customerUserId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            var driver = new Driver { Id = driverId, UserId = driverUserId, Status = DriverStatus.Available };
            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var shipment = new Shipment { Id = shipmentId, CustomerId = customerId, Status = ShipmentStatus.Pending, TrackingNumber = "TRK-NO-DRV" };
            var assignment = new DispatchAssignment { Id = assignmentId, DriverId = driverId, ShipmentId = shipmentId, Status = AssignmentStatus.Pending };

            var dispatchAssignmentRepoMock = new Mock<IGenericRepository<DispatchAssignment>>();
            dispatchAssignmentRepoMock.Setup(r => r.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

            var driverRepoMock = new Mock<IGenericRepository<Driver>>();
            driverRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Driver> { driver }.AsAsyncQueryable());

            var shipmentRepoMock = new Mock<IGenericRepository<Shipment>>();
            shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

            var customerRepoMock = new Mock<IGenericRepository<Customer>>();
            customerRepoMock.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(u => u.UserId).Returns(driverUserId);

            var driverAssignmentServiceMock = new Mock<IDriverAssignmentService>();
            driverAssignmentServiceMock.Setup(s => s.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null); // No alternative driver

            var dispatchAssignmentServiceMock = new Mock<IDispatchAssignmentService>();
            var notifServiceMock = new Mock<INotificationService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var handler = new RejectDispatchAssignmentCommandHandler(
                dispatchAssignmentRepoMock.Object,
                driverRepoMock.Object,
                shipmentRepoMock.Object,
                customerRepoMock.Object,
                notifServiceMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object,
                driverAssignmentServiceMock.Object,
                dispatchAssignmentServiceMock.Object);

            // Act
            await handler.Handle(new RejectDispatchAssignmentCommand(assignmentId), CancellationToken.None);

            // Assert
            assignment.Status.Should().Be(AssignmentStatus.Rejected);
            assignment.RespondedAt.Should().NotBeNull();

            notifServiceMock.Verify(n => n.CreateAsync(
                customerUserId,
                "No Driver Available",
                It.Is<string>(m => m.Contains(shipment.TrackingNumber)),
                NotificationType.NoDriverAvailable,
                It.IsAny<CancellationToken>()), Times.Once);

            notifServiceMock.Verify(n => n.SendRealtimeAsync(
                customerUserId,
                "No Driver Available",
                It.Is<string>(m => m.Contains(shipment.TrackingNumber)),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

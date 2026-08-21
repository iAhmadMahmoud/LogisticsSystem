using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using LogisticsSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Dispatch
{
    public class AssignmentExpirationServiceTests
    {
        private readonly Mock<IGenericRepository<DispatchAssignment>> _assignmentRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<AssignmentExpirationService>> _loggerMock;
        private readonly Mock<IDispatchAssignmentService> _dispatchAssignmentServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IDriverAssignmentService> _driverAssignmentServiceMock;
        private readonly AssignmentExpirationService _service;

        public AssignmentExpirationServiceTests()
        {
            _assignmentRepoMock = new Mock<IGenericRepository<DispatchAssignment>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<AssignmentExpirationService>>();
            _dispatchAssignmentServiceMock = new Mock<IDispatchAssignmentService>();
            _notificationServiceMock = new Mock<INotificationService>();
            _driverAssignmentServiceMock = new Mock<IDriverAssignmentService>();

            var options = Options.Create(new DispatchOptions { AssignmentExpirationMinutes = 10 });

            _service = new AssignmentExpirationService(
                _assignmentRepoMock.Object,
                _unitOfWorkMock.Object,
                options,
                _loggerMock.Object,
                _dispatchAssignmentServiceMock.Object,
                _notificationServiceMock.Object,
                _driverAssignmentServiceMock.Object);
        }

        [Fact]
        public async Task ExpireAssignmentsAsync_WhenNoExpiredAssignments_ReturnsEarly()
        {
            // Arrange
            _assignmentRepoMock
                .Setup(r => r.ListAsync(It.IsAny<ISpecification<DispatchAssignment>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DispatchAssignment>());

            // Act
            await _service.ExpireAssignmentsAsync(CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _dispatchAssignmentServiceMock.Verify(d => d.CreateAssignmentAsync(It.IsAny<Shipment>(), It.IsAny<Driver>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExpireAssignmentsAsync_WhenExpiredAssignmentsExist_MarksExpiredAndReassigns()
        {
            // Arrange
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                TrackingNumber = "TRK-EXP-001"
            };

            var expiredAssignment = new DispatchAssignment
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Shipment = shipment,
                DriverId = Guid.NewGuid(),
                Status = AssignmentStatus.Pending,
                SentAt = DateTime.UtcNow.AddMinutes(-20)
            };

            var alternativeDriver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LicenseNumber = "DL-ALT"
            };

            _assignmentRepoMock
                .Setup(r => r.ListAsync(It.IsAny<ISpecification<DispatchAssignment>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DispatchAssignment> { expiredAssignment });

            _driverAssignmentServiceMock
                .Setup(s => s.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync(alternativeDriver);

            // Act
            await _service.ExpireAssignmentsAsync(CancellationToken.None);

            // Assert
            expiredAssignment.Status.Should().Be(AssignmentStatus.Expired);
            expiredAssignment.RespondedAt.Should().NotBeNull();
            _assignmentRepoMock.Verify(r => r.Update(expiredAssignment), Times.Once);

            _dispatchAssignmentServiceMock.Verify(d => d.CreateAssignmentAsync(shipment, alternativeDriver, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _notificationServiceMock.Verify(n => n.SendRealtimeAsync(
                alternativeDriver.UserId,
                "New Shipment Assignment",
                It.Is<string>(msg => msg.Contains("TRK-EXP-001")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExpireAssignmentsAsync_WhenNoAlternativeDriverFound_MarksExpiredAndContinues()
        {
            // Arrange
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                TrackingNumber = "TRK-EXP-002"
            };

            var expiredAssignment = new DispatchAssignment
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Shipment = shipment,
                DriverId = Guid.NewGuid(),
                Status = AssignmentStatus.Pending,
                SentAt = DateTime.UtcNow.AddMinutes(-20)
            };

            _assignmentRepoMock
                .Setup(r => r.ListAsync(It.IsAny<ISpecification<DispatchAssignment>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DispatchAssignment> { expiredAssignment });

            _driverAssignmentServiceMock
                .Setup(s => s.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null);

            // Act
            await _service.ExpireAssignmentsAsync(CancellationToken.None);

            // Assert
            expiredAssignment.Status.Should().Be(AssignmentStatus.Expired);
            _dispatchAssignmentServiceMock.Verify(d => d.CreateAssignmentAsync(It.IsAny<Shipment>(), It.IsAny<Driver>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Dispatch.Specifications;
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
        private readonly Mock<IGenericRepository<DispatchAssignment>> _assignmentRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ILogger<AssignmentExpirationService>> _loggerMock = new();
        private readonly Mock<IDispatchAssignmentService> _dispatchAssignmentServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IDriverAssignmentService> _driverAssignmentServiceMock = new();
        private readonly IOptions<DispatchOptions> _options;

        private readonly AssignmentExpirationService _service;

        public AssignmentExpirationServiceTests()
        {
            _options = Options.Create(new DispatchOptions { AssignmentExpirationMinutes = 5 });

            _service = new AssignmentExpirationService(
                _assignmentRepoMock.Object,
                _unitOfWorkMock.Object,
                _options,
                _loggerMock.Object,
                _dispatchAssignmentServiceMock.Object,
                _notificationServiceMock.Object,
                _driverAssignmentServiceMock.Object);
        }

        [Fact]
        public async Task ExpireAssignmentsAsync_WhenNoExpiredAssignments_ReturnsWithoutProcessing()
        {
            // Arrange
            _assignmentRepoMock.Setup(x => x.ListAsync(It.IsAny<ExpiredAssignmentsSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DispatchAssignment>());

            // Act
            await _service.ExpireAssignmentsAsync(CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _dispatchAssignmentServiceMock.Verify(x => x.CreateAssignmentAsync(It.IsAny<Shipment>(), It.IsAny<Driver>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExpireAssignmentsAsync_WhenExpiredAssignmentsExist_MarksExpiredAndReassigns()
        {
            // Arrange
            var shipment = new Shipment { Id = Guid.NewGuid(), TrackingNumber = "TRK-EXP-1" };
            var assignment = new DispatchAssignment
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Shipment = shipment,
                DriverId = Guid.NewGuid(),
                AttemptNumber = 1,
                Status = AssignmentStatus.Pending,
                SentAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var nextDriver = new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = DriverStatus.Available };

            _assignmentRepoMock.Setup(x => x.ListAsync(It.IsAny<ExpiredAssignmentsSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DispatchAssignment> { assignment });

            _driverAssignmentServiceMock.Setup(x => x.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync(nextDriver);

            // Act
            await _service.ExpireAssignmentsAsync(CancellationToken.None);

            // Assert
            assignment.Status.Should().Be(AssignmentStatus.Expired);
            assignment.RespondedAt.Should().NotBeNull();

            _assignmentRepoMock.Verify(x => x.Update(assignment), Times.Once);
            _dispatchAssignmentServiceMock.Verify(x => x.CreateAssignmentAsync(shipment, nextDriver, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _notificationServiceMock.Verify(x => x.SendRealtimeAsync(nextDriver.UserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

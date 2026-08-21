using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Dispatch
{
    public class ShipmentAssignmentJobTests
    {
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock;
        private readonly Mock<IDriverAssignmentService> _driverAssignmentServiceMock;
        private readonly Mock<IDispatchAssignmentService> _dispatchAssignmentServiceMock;
        private readonly Mock<ILogger<ShipmentAssignmentJob>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly ShipmentAssignmentJob _job;

        public ShipmentAssignmentJobTests()
        {
            _shipmentRepoMock = new Mock<IGenericRepository<Shipment>>();
            _driverAssignmentServiceMock = new Mock<IDriverAssignmentService>();
            _dispatchAssignmentServiceMock = new Mock<IDispatchAssignmentService>();
            _loggerMock = new Mock<ILogger<ShipmentAssignmentJob>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _job = new ShipmentAssignmentJob(
                _shipmentRepoMock.Object,
                _driverAssignmentServiceMock.Object,
                _dispatchAssignmentServiceMock.Object,
                _loggerMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task AssignShipmentAsync_WhenShipmentNotFound_LogsAndReturnsEarly()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            _shipmentRepoMock
                .Setup(r => r.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Shipment?)null);

            // Act
            await _job.AssignShipmentAsync(shipmentId, CancellationToken.None);

            // Assert
            _driverAssignmentServiceMock.Verify(d => d.FindBestAvailableDriverAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
            _dispatchAssignmentServiceMock.Verify(d => d.CreateAssignmentAsync(It.IsAny<Shipment>(), It.IsAny<Driver>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AssignShipmentAsync_WhenNoDriverFound_LogsAndReturnsEarly()
        {
            // Arrange
            var shipment = new Shipment { Id = Guid.NewGuid() };
            _shipmentRepoMock
                .Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _driverAssignmentServiceMock
                .Setup(d => d.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null);

            // Act
            await _job.AssignShipmentAsync(shipment.Id, CancellationToken.None);

            // Assert
            _dispatchAssignmentServiceMock.Verify(d => d.CreateAssignmentAsync(It.IsAny<Shipment>(), It.IsAny<Driver>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AssignShipmentAsync_WhenDriverFound_CreatesAssignmentAndSaves()
        {
            // Arrange
            var shipment = new Shipment { Id = Guid.NewGuid() };
            var driver = new Driver { Id = Guid.NewGuid() };

            _shipmentRepoMock
                .Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _driverAssignmentServiceMock
                .Setup(d => d.FindBestAvailableDriverAsync(shipment, It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            // Act
            await _job.AssignShipmentAsync(shipment.Id, CancellationToken.None);

            // Assert
            _dispatchAssignmentServiceMock.Verify(d => d.CreateAssignmentAsync(shipment, driver, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

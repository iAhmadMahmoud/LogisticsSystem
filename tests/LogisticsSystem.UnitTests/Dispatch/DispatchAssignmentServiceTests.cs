using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Dispatch
{
    public class DispatchAssignmentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IGenericRepository<DispatchAssignment>> _assignmentRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly DispatchAssignmentService _service;

        public DispatchAssignmentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"DispatchAssignmentTests_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);

            _assignmentRepoMock = new Mock<IGenericRepository<DispatchAssignment>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _notificationServiceMock = new Mock<INotificationService>();

            _assignmentRepoMock.Setup(r => r.AsQueryable()).Returns(_context.DispatchAssignments);

            _service = new DispatchAssignmentService(
                _assignmentRepoMock.Object,
                _unitOfWorkMock.Object,
                _notificationServiceMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateAssignmentAsync_CreatesAssignmentWithCorrectAttemptNumberAndSendsNotification()
        {
            // Arrange
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                TrackingNumber = "TRK-ASSIGN-01",
                CustomerId = Guid.NewGuid()
            };

            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LicenseNumber = "DL-123",
                Status = DriverStatus.Available
            };

            // Seed 1 existing attempt for this shipment
            await _context.DispatchAssignments.AddAsync(new DispatchAssignment
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                DriverId = Guid.NewGuid(),
                AttemptNumber = 1,
                Status = AssignmentStatus.Rejected,
                SentAt = DateTime.UtcNow.AddMinutes(-10)
            });
            await _context.SaveChangesAsync();

            DispatchAssignment? addedAssignment = null;
            _assignmentRepoMock
                .Setup(r => r.AddAsync(It.IsAny<DispatchAssignment>(), It.IsAny<CancellationToken>()))
                .Callback<DispatchAssignment, CancellationToken>((a, _) => addedAssignment = a)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAssignmentAsync(shipment, driver, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.AttemptNumber.Should().Be(2); // First attempt was 1, so new attempt is 2
            result.ShipmentId.Should().Be(shipment.Id);
            result.DriverId.Should().Be(driver.Id);
            result.Status.Should().Be(AssignmentStatus.Pending);

            _assignmentRepoMock.Verify(r => r.AddAsync(It.IsAny<DispatchAssignment>(), It.IsAny<CancellationToken>()), Times.Once);

            _notificationServiceMock.Verify(n => n.CreateAsync(
                driver.UserId,
                "New Shipment Assignment",
                It.Is<string>(msg => msg.Contains("TRK-ASSIGN-01")),
                NotificationType.DispatchAssignmentReceived,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

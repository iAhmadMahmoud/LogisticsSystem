using AutoMapper;
using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.Queries.GetShipmentStatusHistory;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.ShipmentStatusHistories
{
    public class GetShipmentStatusHistoryQueryHandlerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock;
        private readonly Mock<IGenericRepository<ShipmentStatusHistory>> _statusHistoryRepoMock;
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock;
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetShipmentStatusHistoryQueryHandler _handler;

        public GetShipmentStatusHistoryQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"StatusHistoryTests_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);

            _shipmentRepoMock = new Mock<IGenericRepository<Shipment>>();
            _statusHistoryRepoMock = new Mock<IGenericRepository<ShipmentStatusHistory>>();
            _customerRepoMock = new Mock<IGenericRepository<Customer>>();
            _driverRepoMock = new Mock<IGenericRepository<Driver>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _mapperMock = new Mock<IMapper>();

            _statusHistoryRepoMock.Setup(r => r.AsQueryable()).Returns(_context.ShipmentStatusHistories);
            _customerRepoMock.Setup(r => r.AsQueryable()).Returns(_context.Customers);
            _driverRepoMock.Setup(r => r.AsQueryable()).Returns(_context.Drivers);

            _handler = new GetShipmentStatusHistoryQueryHandler(
                _shipmentRepoMock.Object,
                _statusHistoryRepoMock.Object,
                _customerRepoMock.Object,
                _driverRepoMock.Object,
                _currentUserServiceMock.Object,
                _mapperMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Handle_WhenShipmentNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            _shipmentRepoMock
                .Setup(r => r.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Shipment?)null);

            // Act
            var act = () => _handler.Handle(new GetShipmentStatusHistoryQuery(shipmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task Handle_WhenAdmin_ReturnsChronologicalHistory()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var shipment = new Shipment { Id = shipmentId };

            _shipmentRepoMock
                .Setup(r => r.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Admin)).Returns(true);

            var now = DateTime.UtcNow;
            await _context.ShipmentStatusHistories.AddRangeAsync(
                new ShipmentStatusHistory { Id = Guid.NewGuid(), ShipmentId = shipmentId, Status = ShipmentStatus.Delivered, ChangedAt = now.AddMinutes(10) },
                new ShipmentStatusHistory { Id = Guid.NewGuid(), ShipmentId = shipmentId, Status = ShipmentStatus.Pending, ChangedAt = now }
            );
            await _context.SaveChangesAsync();

            _mapperMock
                .Setup(m => m.Map<IReadOnlyList<ShipmentStatusHistoryDto>>(It.IsAny<List<ShipmentStatusHistory>>()))
                .Returns<List<ShipmentStatusHistory>>(list => list.Select(h => new ShipmentStatusHistoryDto { Status = h.Status, ChangedAt = h.ChangedAt }).ToList());

            // Act
            var result = await _handler.Handle(new GetShipmentStatusHistoryQuery(shipmentId), CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result[0].Status.Should().Be(ShipmentStatus.Pending);
            result[1].Status.Should().Be(ShipmentStatus.Delivered);
        }

        [Fact]
        public async Task Handle_WhenCustomerAccessesOtherCustomerShipment_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var otherCustomerId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var shipment = new Shipment { Id = shipmentId, CustomerId = otherCustomerId };

            _shipmentRepoMock
                .Setup(r => r.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Dispatcher)).Returns(false);
            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Customer)).Returns(true);
            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

            await _context.Customers.AddAsync(new Customer { Id = customerId, UserId = userId });
            await _context.SaveChangesAsync();

            // Act
            var act = () => _handler.Handle(new GetShipmentStatusHistoryQuery(shipmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_WhenDriverAccessesUnassignedShipment_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var otherDriverId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var shipment = new Shipment { Id = shipmentId, DriverId = otherDriverId };

            _shipmentRepoMock
                .Setup(r => r.GetByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Dispatcher)).Returns(false);
            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Customer)).Returns(false);
            _currentUserServiceMock.Setup(c => c.IsInRole(Roles.Driver)).Returns(true);
            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

            await _context.Drivers.AddAsync(new Driver { Id = driverId, UserId = userId, LicenseNumber = "DL-1" });
            await _context.SaveChangesAsync();

            // Act
            var act = () => _handler.Handle(new GetShipmentStatusHistoryQuery(shipmentId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}

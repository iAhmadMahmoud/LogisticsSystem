using FluentAssertions;
using LogisticsSystem.Application.Features.Dashboard.Queries.GetRecentActivity;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsSystem.UnitTests.Dashboard
{
    public class GetRecentActivityQueryHandlerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GetRecentActivityQueryHandler _handler;

        public GetRecentActivityQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"RecentActivityTests_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _handler = new GetRecentActivityQueryHandler(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Handle_WhenNoActivityExists_ReturnsEmptyPagedResult()
        {
            // Act
            var result = await _handler.Handle(new GetRecentActivityQuery(1, 10), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WhenActivityExists_ReturnsSortedNewestFirstAndPaginated()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var shipment = new Shipment
            {
                Id = shipmentId,
                TrackingNumber = "TRK-100",
                CustomerId = Guid.NewGuid()
            };

            var now = DateTime.UtcNow;

            var history1 = new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                Shipment = shipment,
                Status = ShipmentStatus.Pending,
                ChangedAt = now.AddMinutes(-30)
            };

            var history2 = new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                Shipment = shipment,
                Status = ShipmentStatus.Assigned,
                ChangedAt = now.AddMinutes(-20)
            };

            var history3 = new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                Shipment = shipment,
                Status = ShipmentStatus.Delivered,
                ChangedAt = now.AddMinutes(-10)
            };

            await _context.Shipments.AddAsync(shipment);
            await _context.ShipmentStatusHistories.AddRangeAsync(history1, history2, history3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetRecentActivityQuery(1, 2), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(3);
            result.Items.Should().HaveCount(2);
            result.Items[0].ActivityType.Should().Be("ShipmentDelivered");
            result.Items[0].Description.Should().Contain("TRK-100");
            result.Items[1].ActivityType.Should().Be("ShipmentAssigned");
        }

        [Fact]
        public async Task Handle_WithActivityTypeFilter_ReturnsOnlyMatchingActivities()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var shipment = new Shipment
            {
                Id = shipmentId,
                TrackingNumber = "TRK-200",
                CustomerId = Guid.NewGuid()
            };

            var now = DateTime.UtcNow;

            var history1 = new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                Shipment = shipment,
                Status = ShipmentStatus.Pending,
                ChangedAt = now.AddMinutes(-30)
            };

            var history2 = new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                Shipment = shipment,
                Status = ShipmentStatus.Delivered,
                ChangedAt = now.AddMinutes(-10)
            };

            await _context.Shipments.AddAsync(shipment);
            await _context.ShipmentStatusHistories.AddRangeAsync(history1, history2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetRecentActivityQuery(1, 10, "ShipmentDelivered"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].ActivityType.Should().Be("ShipmentDelivered");
        }
    }
}

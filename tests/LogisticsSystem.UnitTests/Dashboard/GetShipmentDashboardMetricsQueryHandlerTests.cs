using FluentAssertions;
using LogisticsSystem.Application.Features.Dashboard.Queries.GetShipmentDashboardMetrics;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsSystem.UnitTests.Dashboard
{
    public class GetShipmentDashboardMetricsQueryHandlerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GetShipmentDashboardMetricsQueryHandler _handler;

        public GetShipmentDashboardMetricsQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"DashboardTests_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _handler = new GetShipmentDashboardMetricsQueryHandler(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Handle_WhenNoShipmentsExist_ReturnsAllZeroMetrics()
        {
            // Act
            var result = await _handler.Handle(new GetShipmentDashboardMetricsQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalShipments.Should().Be(0);
            result.PendingShipments.Should().Be(0);
            result.AssignedShipments.Should().Be(0);
            result.PickedUpShipments.Should().Be(0);
            result.InTransitShipments.Should().Be(0);
            result.DeliveredShipments.Should().Be(0);
            result.CancelledShipments.Should().Be(0);
            result.FailedShipments.Should().Be(0);
            result.ShipmentsCreatedToday.Should().Be(0);
            result.ShipmentsDeliveredToday.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenShipmentsExist_AggregatesCountsCorrectly()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var yesterday = today.AddDays(-1);

            var customerId = Guid.NewGuid();

            var shipments = new List<Shipment>
            {
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.Pending,
                    CreatedAt = today
                },
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.Assigned,
                    CreatedAt = today
                },
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.PickedUp,
                    CreatedAt = yesterday
                },
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.InTransit,
                    CreatedAt = yesterday
                },
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.Delivered,
                    DeliveredAt = today,
                    CreatedAt = yesterday
                },
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.Delivered,
                    DeliveredAt = yesterday,
                    CreatedAt = yesterday.AddDays(-1)
                },
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.Cancelled,
                    CreatedAt = today
                },
                new Shipment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = ShipmentStatus.Failed,
                    CreatedAt = yesterday
                }
            };

            await _context.Shipments.AddRangeAsync(shipments);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetShipmentDashboardMetricsQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalShipments.Should().Be(8);
            result.PendingShipments.Should().Be(1);
            result.AssignedShipments.Should().Be(1);
            result.PickedUpShipments.Should().Be(1);
            result.InTransitShipments.Should().Be(1);
            result.DeliveredShipments.Should().Be(2);
            result.CancelledShipments.Should().Be(1);
            result.FailedShipments.Should().Be(1);
            result.ShipmentsCreatedToday.Should().Be(3); // 1 Pending + 1 Assigned + 1 Cancelled
            result.ShipmentsDeliveredToday.Should().Be(1); // 1 Delivered with DeliveredAt = today
        }

        [Fact]
        public async Task Handle_WhenAllShipmentsInSameStatus_CalculatesAccurately()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var shipments = new List<Shipment>
            {
                new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, Status = ShipmentStatus.Pending, CreatedAt = DateTime.UtcNow },
                new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, Status = ShipmentStatus.Pending, CreatedAt = DateTime.UtcNow }
            };

            await _context.Shipments.AddRangeAsync(shipments);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetShipmentDashboardMetricsQuery(), CancellationToken.None);

            // Assert
            result.TotalShipments.Should().Be(2);
            result.PendingShipments.Should().Be(2);
            result.DeliveredShipments.Should().Be(0);
            result.ShipmentsCreatedToday.Should().Be(2);
        }
    }
}

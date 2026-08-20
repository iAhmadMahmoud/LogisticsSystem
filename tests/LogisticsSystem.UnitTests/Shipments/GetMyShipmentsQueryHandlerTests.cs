using AutoMapper;
using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.Shipments.Queries.GetMyShipments;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Shipments
{
    public class GetMyShipmentsQueryHandlerTests
    {
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock = new();
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        private readonly GetMyShipmentsQueryHandler _handler;

        public GetMyShipmentsQueryHandlerTests()
        {
            _handler = new GetMyShipmentsQueryHandler(
                _shipmentRepoMock.Object,
                _customerRepoMock.Object,
                _currentUserServiceMock.Object,
                _mapperMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCustomerExists_ReturnsPagedShipments()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var customer = new Customer { Id = customerId, UserId = userId };

            var shipments = new List<Shipment>
            {
                new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, TrackingNumber = "TRK-001", Status = ShipmentStatus.Pending },
                new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, TrackingNumber = "TRK-002", Status = ShipmentStatus.InTransit }
            };

            var shipmentDtos = new List<ShipmentDto>
            {
                new ShipmentDto { Id = shipments[0].Id, TrackingNumber = "TRK-001" },
                new ShipmentDto { Id = shipments[1].Id, TrackingNumber = "TRK-002" }
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _shipmentRepoMock.Setup(x => x.CountAsync(It.IsAny<MyShipmentsSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);
            _shipmentRepoMock.Setup(x => x.ListAsync(It.IsAny<MyShipmentsSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipments);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<ShipmentDto>>(shipments))
                .Returns(shipmentDtos);

            var query = new GetMyShipmentsQuery(PageNumber: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WhenCustomerNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            var query = new GetMyShipmentsQuery();

            // Act
            var act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Customer profile not found.");
        }
    }
}

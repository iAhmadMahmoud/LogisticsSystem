using AutoMapper;
using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.Shipments.Queries.GetShipmentById;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Shipments
{
    public class GetShipmentByIdHandlerTests
    {
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepoMock = new();
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        private readonly GetShipmentByIdHandler _handler;

        public GetShipmentByIdHandlerTests()
        {
            _handler = new GetShipmentByIdHandler(
                _shipmentRepoMock.Object,
                _mapperMock.Object,
                _customerRepoMock.Object,
                _currentUserServiceMock.Object,
                _identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCustomerViewsShipmentWithDriverAndTracking_ReturnsEnhancedDto()
        {
            // Arrange
            var customerUserId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var driverUserId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();

            var customer = new Customer { Id = customerId, UserId = customerUserId };
            var driver = new Driver
            {
                Id = driverId,
                UserId = driverUserId,
                LicenseNumber = "DL-998877",
                Latitude = 30.05,
                Longitude = 31.25
            };

            var tracking = new ShipmentTracking
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                Latitude = 30.04,
                Longitude = 31.24,
                RecordedAt = DateTime.UtcNow
            };

            var shipment = new Shipment
            {
                Id = shipmentId,
                CustomerId = customerId,
                DriverId = driverId,
                Driver = driver,
                TrackingNumber = "TRK-ENHANCED-001",
                Status = ShipmentStatus.InTransit,
                ShipmentTrackings = new List<ShipmentTracking> { tracking }
            };

            var shipmentDto = new ShipmentDto
            {
                Id = shipmentId,
                TrackingNumber = "TRK-ENHANCED-001",
                Status = ShipmentStatus.InTransit,
                DriverId = driverId
            };

            _currentUserServiceMock.Setup(x => x.IsInRole(Roles.Customer)).Returns(true);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(customerUserId);

            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _shipmentRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<ShipmentByIdAndCustomerSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipment);

            _mapperMock.Setup(x => x.Map<ShipmentDto>(shipment))
                .Returns(shipmentDto);

            _identityServiceMock.Setup(x => x.GetUserByIdAsync(driverUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfoDto(driverUserId, "Alex Driver", "alex@driver.com", "+1234567890"));

            // Act
            var result = await _handler.Handle(new GetShipmentByIdQuery(shipmentId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.DriverLicense.Should().Be("DL-998877");
            result.DriverName.Should().Be("Alex Driver");
            result.DriverPhone.Should().Be("+1234567890");
            result.LatestLocation.Should().NotBeNull();
            result.LatestLocation!.Latitude.Should().Be(30.04);
            result.LatestLocation!.Longitude.Should().Be(31.24);
        }

        [Fact]
        public async Task Handle_WhenCustomerProfileNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.IsInRole(Roles.Customer)).Returns(true);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            // Act
            var act = async () => await _handler.Handle(new GetShipmentByIdQuery(Guid.NewGuid()), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Customer profile not found.");
        }

        [Fact]
        public async Task Handle_WhenShipmentNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.IsInRole(Roles.Customer)).Returns(false);
            _shipmentRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<ShipmentByIdWithDetailsSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Shipment?)null);

            // Act
            var act = async () => await _handler.Handle(new GetShipmentByIdQuery(Guid.NewGuid()), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Shipment not found.");
        }
    }
}

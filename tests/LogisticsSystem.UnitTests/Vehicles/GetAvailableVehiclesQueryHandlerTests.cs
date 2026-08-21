using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetAvailableVehicles;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class GetAvailableVehiclesQueryHandlerTests
    {
        private readonly Mock<IGenericRepository<Vehicle>> _vehicleRepoMock = new();
        private readonly GetAvailableVehiclesQueryHandler _handler;

        public GetAvailableVehiclesQueryHandlerTests()
        {
            _handler = new GetAvailableVehiclesQueryHandler(_vehicleRepoMock.Object);
        }

        [Fact]
        public async Task Handle_WhenAvailableVehiclesExist_ReturnsPagedResult()
        {
            // Arrange
            var vehicle1 = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "ABC-123",
                Brand = "Toyota",
                Model = "Hilux",
                ManufacturingYear = 2022,
                Color = "White",
                Type = VehicleType.Truck,
                Capacity = 1500,
                IsActive = true,
                Driver = null,
                CreatedAt = DateTime.UtcNow
            };

            var vehicle2 = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "XYZ-789",
                Brand = "Mercedes",
                Model = "Sprinter",
                ManufacturingYear = 2023,
                Color = "Silver",
                Type = VehicleType.Van,
                Capacity = 2500,
                IsActive = true,
                Driver = null,
                CreatedAt = DateTime.UtcNow
            };

            var vehicles = new List<Vehicle> { vehicle1, vehicle2 };

            _vehicleRepoMock.Setup(x => x.CountAsync(
                    It.IsAny<AvailableVehiclesSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            _vehicleRepoMock.Setup(x => x.ListAsync(
                    It.IsAny<AvailableVehiclesSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicles);

            var query = new GetAvailableVehiclesQuery(PageNumber: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.Items.Should().HaveCount(2);

            var first = result.Items[0];
            first.Id.Should().Be(vehicle1.Id);
            first.PlateNumber.Should().Be("ABC-123");
            first.IsAssigned.Should().BeFalse();
            first.DriverId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenNoAvailableVehicles_ReturnsEmptyPagedResult()
        {
            // Arrange
            _vehicleRepoMock.Setup(x => x.CountAsync(
                    It.IsAny<AvailableVehiclesSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _vehicleRepoMock.Setup(x => x.ListAsync(
                    It.IsAny<AvailableVehiclesSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Vehicle>());

            var query = new GetAvailableVehiclesQuery(PageNumber: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(0);
            result.Items.Should().BeEmpty();
        }
    }
}

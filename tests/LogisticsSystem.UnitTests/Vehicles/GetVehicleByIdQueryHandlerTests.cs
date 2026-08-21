using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicleById;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class GetVehicleByIdQueryHandlerTests
    {
        private readonly Mock<IGenericRepository<Vehicle>> _vehicleRepoMock = new();
        private readonly GetVehicleByIdQueryHandler _handler;

        public GetVehicleByIdQueryHandlerTests()
        {
            _handler = new GetVehicleByIdQueryHandler(_vehicleRepoMock.Object);
        }

        [Fact]
        public async Task Handle_WhenVehicleDoesNotExist_ThrowsKeyNotFoundException()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            // Act
            var act = async () => await _handler.Handle(new GetVehicleByIdQuery(vehicleId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Vehicle not found.");
        }

        [Fact]
        public async Task Handle_WhenVehicleExists_ReturnsVehicleDto()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var vehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "ABC-1234",
                Brand = "Mercedes",
                Model = "Actros",
                ManufacturingYear = 2023,
                Color = "Silver",
                Type = VehicleType.Truck,
                Capacity = 20000m,
                IsActive = true,
                Driver = new Driver { Id = driverId }
            };

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            // Act
            var result = await _handler.Handle(new GetVehicleByIdQuery(vehicleId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(vehicleId);
            result.PlateNumber.Should().Be("ABC-1234");
            result.Brand.Should().Be("Mercedes");
            result.DriverId.Should().Be(driverId);
            result.IsAssigned.Should().BeTrue();
        }
    }
}

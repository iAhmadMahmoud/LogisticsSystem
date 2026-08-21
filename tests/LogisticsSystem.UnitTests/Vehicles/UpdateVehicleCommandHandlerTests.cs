using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Vehicles.Commands.UpdateVehicle;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class UpdateVehicleCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Vehicle>> _vehicleRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly UpdateVehicleCommandHandler _handler;

        public UpdateVehicleCommandHandlerTests()
        {
            _handler = new UpdateVehicleCommandHandler(_vehicleRepoMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenVehicleDoesNotExist_ThrowsKeyNotFoundException()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            var command = new UpdateVehicleCommand(
                vehicleId,
                "ABC-1234",
                "Volvo",
                "FH16",
                2022,
                "Blue",
                VehicleType.Truck,
                25000m,
                true);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Vehicle not found.");
        }

        [Fact]
        public async Task Handle_WhenUpdatingToPlateNumberBelongingToAnotherVehicle_ThrowsInvalidOperationException()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            var otherVehicleId = Guid.NewGuid();

            var currentVehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "OLD-1111",
                Brand = "Volvo"
            };

            var otherVehicleWithSamePlate = new Vehicle
            {
                Id = otherVehicleId,
                PlateNumber = "NEW-2222"
            };

            var command = new UpdateVehicleCommand(
                vehicleId,
                "NEW-2222",
                "Volvo",
                "FH16",
                2022,
                "Blue",
                VehicleType.Truck,
                25000m,
                true);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentVehicle);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByPlateNumberSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherVehicleWithSamePlate);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task Handle_WhenValidParameters_UpdatesVehicleAndReturnsDto()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            var driverId = Guid.NewGuid();

            var currentVehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "ABC-1234",
                Brand = "Volvo",
                Model = "FH12",
                Driver = new Driver { Id = driverId }
            };

            var command = new UpdateVehicleCommand(
                vehicleId,
                "ABC-1234",
                "Volvo",
                "FH16",
                2023,
                "Silver",
                VehicleType.Truck,
                30000m,
                true);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentVehicle);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Model.Should().Be("FH16");
            result.ManufacturingYear.Should().Be(2023);
            result.Color.Should().Be("Silver");
            result.DriverId.Should().Be(driverId);

            _vehicleRepoMock.Verify(x => x.Update(currentVehicle), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

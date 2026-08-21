using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Vehicles.Commands.CreateVehicle;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class CreateVehicleCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Vehicle>> _vehicleRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly CreateVehicleCommandHandler _handler;

        public CreateVehicleCommandHandlerTests()
        {
            _handler = new CreateVehicleCommandHandler(_vehicleRepoMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenPlateNumberIsUnique_CreatesVehicleAndReturnsDto()
        {
            // Arrange
            var command = new CreateVehicleCommand(
                "ABC-1234",
                "Mercedes-Benz",
                "Sprinter",
                2023,
                "White",
                VehicleType.Van,
                1500m);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<ISpecification<Vehicle>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.PlateNumber.Should().Be("ABC-1234");
            result.Brand.Should().Be("Mercedes-Benz");
            result.Model.Should().Be("Sprinter");
            result.IsActive.Should().BeTrue();
            result.DriverId.Should().BeNull();

            _vehicleRepoMock.Verify(x => x.AddAsync(It.Is<Vehicle>(v => v.PlateNumber == "ABC-1234"), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenPlateNumberAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var command = new CreateVehicleCommand(
                "ABC-1234",
                "Mercedes-Benz",
                "Sprinter",
                2023,
                "White",
                VehicleType.Van,
                1500m);

            var existingVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "ABC-1234"
            };

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<ISpecification<Vehicle>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVehicle);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");

            _vehicleRepoMock.Verify(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

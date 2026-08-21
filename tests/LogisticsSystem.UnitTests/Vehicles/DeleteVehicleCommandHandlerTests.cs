using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Vehicles.Commands.DeleteVehicle;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class DeleteVehicleCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Vehicle>> _vehicleRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly DeleteVehicleCommandHandler _handler;

        public DeleteVehicleCommandHandlerTests()
        {
            _handler = new DeleteVehicleCommandHandler(_vehicleRepoMock.Object, _unitOfWorkMock.Object);
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
            var act = async () => await _handler.Handle(new DeleteVehicleCommand(vehicleId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Vehicle not found.");
        }

        [Fact]
        public async Task Handle_WhenVehicleIsAssignedToDriver_ThrowsDomainException()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            var vehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "ABC-1234",
                Driver = new Driver { Id = Guid.NewGuid() }
            };

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            // Act
            var act = async () => await _handler.Handle(new DeleteVehicleCommand(vehicleId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Cannot delete a vehicle that is currently assigned to a driver.");

            _vehicleRepoMock.Verify(x => x.Delete(It.IsAny<Vehicle>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenVehicleIsUnassigned_DeletesVehicleSuccessfully()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            var vehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "ABC-1234",
                Driver = null
            };

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            // Act
            await _handler.Handle(new DeleteVehicleCommand(vehicleId), CancellationToken.None);

            // Assert
            _vehicleRepoMock.Verify(x => x.Delete(vehicle), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

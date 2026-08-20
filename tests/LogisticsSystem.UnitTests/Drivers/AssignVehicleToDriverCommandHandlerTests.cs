using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Drivers.Commands.AssignVehicleToDriver;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Drivers
{
    public class AssignVehicleToDriverCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock = new();
        private readonly Mock<IGenericRepository<Vehicle>> _vehicleRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly AssignVehicleToDriverCommandHandler _handler;

        public AssignVehicleToDriverCommandHandlerTests()
        {
            _handler = new AssignVehicleToDriverCommandHandler(
                _driverRepoMock.Object,
                _vehicleRepoMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenDriverNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null);

            var command = new AssignVehicleToDriverCommand(driverId, vehicleId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Driver not found.");
        }

        [Fact]
        public async Task Handle_WhenDriverIsSuspended_ThrowsDomainException()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();
            var driver = new Driver
            {
                Id = driverId,
                Status = DriverStatus.Suspended
            };

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            var command = new AssignVehicleToDriverCommand(driverId, vehicleId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Cannot assign a vehicle to a suspended driver.");
        }

        [Fact]
        public async Task Handle_WhenDriverAlreadyHasAnotherVehicle_ThrowsDomainException()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var existingVehicleId = Guid.NewGuid();
            var newVehicleId = Guid.NewGuid();

            var driver = new Driver
            {
                Id = driverId,
                Status = DriverStatus.Available,
                VehicleId = existingVehicleId
            };

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            var command = new AssignVehicleToDriverCommand(driverId, newVehicleId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Driver already has an assigned vehicle.");
        }

        [Fact]
        public async Task Handle_WhenVehicleNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();
            var driver = new Driver
            {
                Id = driverId,
                Status = DriverStatus.Available,
                VehicleId = null
            };

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            var command = new AssignVehicleToDriverCommand(driverId, vehicleId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Vehicle not found.");
        }

        [Fact]
        public async Task Handle_WhenVehicleIsInactive_ThrowsDomainException()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();
            var driver = new Driver
            {
                Id = driverId,
                Status = DriverStatus.Available,
                VehicleId = null
            };

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                IsActive = false,
                Driver = null
            };

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            var command = new AssignVehicleToDriverCommand(driverId, vehicleId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Cannot assign an inactive vehicle.");
        }

        [Fact]
        public async Task Handle_WhenVehicleAssignedToAnotherDriver_ThrowsDomainException()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var anotherDriverId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();

            var driver = new Driver
            {
                Id = driverId,
                Status = DriverStatus.Available,
                VehicleId = null
            };

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                IsActive = true,
                Driver = new Driver { Id = anotherDriverId }
            };

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            var command = new AssignVehicleToDriverCommand(driverId, vehicleId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Vehicle is already assigned to another driver.");
        }

        [Fact]
        public async Task Handle_WhenAssignmentIsValid_AssignsVehicleAndSaves()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();

            var driver = new Driver
            {
                Id = driverId,
                Status = DriverStatus.Available,
                VehicleId = null
            };

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                IsActive = true,
                Driver = null
            };

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            _vehicleRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<VehicleByIdWithDriverSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            var command = new AssignVehicleToDriverCommand(driverId, vehicleId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            driver.VehicleId.Should().Be(vehicleId);
            _driverRepoMock.Verify(x => x.Update(driver), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

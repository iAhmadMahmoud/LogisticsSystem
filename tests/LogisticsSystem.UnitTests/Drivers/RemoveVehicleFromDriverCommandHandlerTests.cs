using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Drivers.Commands.RemoveVehicleFromDriver;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Drivers
{
    public class RemoveVehicleFromDriverCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Driver>> _driverRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly RemoveVehicleFromDriverCommandHandler _handler;

        public RemoveVehicleFromDriverCommandHandlerTests()
        {
            _handler = new RemoveVehicleFromDriverCommandHandler(
                _driverRepoMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenDriverNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var driverId = Guid.NewGuid();

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null);

            var command = new RemoveVehicleFromDriverCommand(driverId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Driver not found.");
        }

        [Fact]
        public async Task Handle_WhenDriverHasNoAssignedVehicle_ThrowsDomainException()
        {
            // Arrange
            var driverId = Guid.NewGuid();
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

            var command = new RemoveVehicleFromDriverCommand(driverId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Driver does not have an assigned vehicle.");
        }

        [Fact]
        public async Task Handle_WhenDriverHasAssignedVehicle_RemovesVehicleAndSaves()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();
            var driver = new Driver
            {
                Id = driverId,
                Status = DriverStatus.Available,
                VehicleId = vehicleId
            };

            _driverRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<DriverByIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);

            var command = new RemoveVehicleFromDriverCommand(driverId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            driver.VehicleId.Should().BeNull();
            _driverRepoMock.Verify(x => x.Update(driver), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

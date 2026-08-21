using FluentAssertions;
using LogisticsSystem.Application.Features.Drivers.Commands.AssignVehicleToDriver;
using Xunit;

namespace LogisticsSystem.UnitTests.Drivers
{
    public class AssignVehicleToDriverCommandValidatorTests
    {
        private readonly AssignVehicleToDriverCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPassValidation()
        {
            // Arrange
            var command = new AssignVehicleToDriverCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenDriverIdIsEmpty_ShouldFailValidation()
        {
            // Arrange
            var command = new AssignVehicleToDriverCommand(Guid.Empty, Guid.NewGuid());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.DriverId));
        }

        [Fact]
        public void Validate_WhenVehicleIdIsEmpty_ShouldFailValidation()
        {
            // Arrange
            var command = new AssignVehicleToDriverCommand(Guid.NewGuid(), Guid.Empty);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.VehicleId));
        }
    }
}

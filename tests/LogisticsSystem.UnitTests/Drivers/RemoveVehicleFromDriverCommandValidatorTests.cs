using FluentAssertions;
using LogisticsSystem.Application.Features.Drivers.Commands.RemoveVehicleFromDriver;
using Xunit;

namespace LogisticsSystem.UnitTests.Drivers
{
    public class RemoveVehicleFromDriverCommandValidatorTests
    {
        private readonly RemoveVehicleFromDriverCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidDriverId_ShouldPassValidation()
        {
            // Arrange
            var command = new RemoveVehicleFromDriverCommand(Guid.NewGuid());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenDriverIdIsEmpty_ShouldFailValidation()
        {
            // Arrange
            var command = new RemoveVehicleFromDriverCommand(Guid.Empty);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.DriverId));
        }
    }
}

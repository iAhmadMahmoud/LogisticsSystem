using FluentAssertions;
using LogisticsSystem.Application.Features.Vehicles.Commands.UpdateVehicle;
using LogisticsSystem.Domain.Enums;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class UpdateVehicleCommandValidatorTests
    {
        private readonly UpdateVehicleCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPass()
        {
            // Arrange
            var command = new UpdateVehicleCommand(
                Guid.NewGuid(),
                "ABC-1234",
                "Volvo",
                "FH16",
                2022,
                "Blue",
                VehicleType.Truck,
                25000m,
                true);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenIdIsEmpty_ShouldFail()
        {
            // Arrange
            var command = new UpdateVehicleCommand(
                Guid.Empty,
                "ABC-1234",
                "Volvo",
                "FH16",
                2022,
                "Blue",
                VehicleType.Truck,
                25000m,
                true);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_WhenPlateNumberIsEmpty_ShouldFail(string plateNumber)
        {
            // Arrange
            var command = new UpdateVehicleCommand(
                Guid.NewGuid(),
                plateNumber,
                "Volvo",
                "FH16",
                2022,
                "Blue",
                VehicleType.Truck,
                25000m,
                true);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.PlateNumber));
        }
    }
}

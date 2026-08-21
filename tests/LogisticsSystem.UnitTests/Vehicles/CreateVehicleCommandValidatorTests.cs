using FluentAssertions;
using LogisticsSystem.Application.Features.Vehicles.Commands.CreateVehicle;
using LogisticsSystem.Domain.Enums;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class CreateVehicleCommandValidatorTests
    {
        private readonly CreateVehicleCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPass()
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

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_WhenPlateNumberIsEmpty_ShouldFail(string plateNumber)
        {
            // Arrange
            var command = new CreateVehicleCommand(
                plateNumber,
                "Mercedes-Benz",
                "Sprinter",
                2023,
                "White",
                VehicleType.Van,
                1500m);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.PlateNumber));
        }

        [Theory]
        [InlineData(1900)]
        [InlineData(1899)]
        public void Validate_WhenManufacturingYearTooOld_ShouldFail(int year)
        {
            // Arrange
            var command = new CreateVehicleCommand(
                "ABC-1234",
                "Mercedes-Benz",
                "Sprinter",
                year,
                "White",
                VehicleType.Van,
                1500m);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.ManufacturingYear));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Validate_WhenCapacityNotPositive_ShouldFail(decimal capacity)
        {
            // Arrange
            var command = new CreateVehicleCommand(
                "ABC-1234",
                "Mercedes-Benz",
                "Sprinter",
                2023,
                "White",
                VehicleType.Van,
                capacity);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Capacity));
        }
    }
}

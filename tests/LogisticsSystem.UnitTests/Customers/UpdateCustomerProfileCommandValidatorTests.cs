using FluentAssertions;
using LogisticsSystem.Application.Features.Customers.Commands.UpdateCustomerProfile;
using Xunit;

namespace LogisticsSystem.UnitTests.Customers
{
    public class UpdateCustomerProfileCommandValidatorTests
    {
        private readonly UpdateCustomerProfileCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidCommand_PassesValidation()
        {
            // Arrange
            var command = new UpdateCustomerProfileCommand("John", "Doe", "+1234567890", "123 Main St");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "Doe")]
        [InlineData(null, "Doe")]
        [InlineData("John", "")]
        [InlineData("John", null)]
        public void Validate_MissingRequiredNames_FailsValidation(string? firstName, string? lastName)
        {
            // Arrange
            var command = new UpdateCustomerProfileCommand(firstName!, lastName!, "+1234567890", "123 Main St");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_ExcessiveLengths_FailsValidation()
        {
            // Arrange
            var command = new UpdateCustomerProfileCommand(
                new string('a', 51),
                new string('b', 51),
                new string('1', 21),
                new string('c', 201));

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(4);
        }
    }
}

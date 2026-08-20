using FluentAssertions;
using LogisticsSystem.Application.Features.Users.Commands.UpdateUser;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class UpdateUserCommandValidatorTests
    {
        private readonly UpdateUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPass()
        {
            // Arrange
            var command = new UpdateUserCommand(
                Guid.NewGuid(),
                "John",
                "Doe",
                "+1234567890",
                "john.doe@example.com",
                "johndoe");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenIdIsEmpty_ShouldFail()
        {
            // Arrange
            var command = new UpdateUserCommand(
                Guid.Empty,
                "John",
                "Doe",
                null,
                "john@example.com",
                "johndoe");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid-email")]
        public void Validate_WhenEmailIsInvalid_ShouldFail(string email)
        {
            // Arrange
            var command = new UpdateUserCommand(
                Guid.NewGuid(),
                "John",
                "Doe",
                null,
                email,
                "johndoe");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Email));
        }
    }
}

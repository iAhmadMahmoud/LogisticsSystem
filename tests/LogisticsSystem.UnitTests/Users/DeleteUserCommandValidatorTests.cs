using FluentAssertions;
using LogisticsSystem.Application.Features.Users.Commands.DeleteUser;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class DeleteUserCommandValidatorTests
    {
        private readonly DeleteUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenIdIsValid_ShouldPass()
        {
            // Arrange
            var command = new DeleteUserCommand(Guid.NewGuid());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenIdIsEmpty_ShouldFail()
        {
            // Arrange
            var command = new DeleteUserCommand(Guid.Empty);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
        }
    }
}

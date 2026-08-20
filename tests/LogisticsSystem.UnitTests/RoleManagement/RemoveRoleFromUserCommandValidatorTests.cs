using FluentAssertions;
using LogisticsSystem.Application.Features.RoleManagement.Commands.RemoveRoleFromUser;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class RemoveRoleFromUserCommandValidatorTests
    {
        private readonly RemoveRoleFromUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPass()
        {
            // Arrange
            var command = new RemoveRoleFromUserCommand(Guid.NewGuid(), "Dispatcher");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_ShouldFail()
        {
            // Arrange
            var command = new RemoveRoleFromUserCommand(Guid.Empty, "Dispatcher");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UserId));
        }

        [Fact]
        public void Validate_WhenRoleNameIsEmpty_ShouldFail()
        {
            // Arrange
            var command = new RemoveRoleFromUserCommand(Guid.NewGuid(), "");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.RoleName));
        }
    }
}

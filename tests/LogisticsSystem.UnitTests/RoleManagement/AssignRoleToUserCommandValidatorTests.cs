using FluentAssertions;
using LogisticsSystem.Application.Features.RoleManagement.Commands.AssignRoleToUser;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class AssignRoleToUserCommandValidatorTests
    {
        private readonly AssignRoleToUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPass()
        {
            // Arrange
            var command = new AssignRoleToUserCommand(Guid.NewGuid(), "Dispatcher");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_ShouldFail()
        {
            // Arrange
            var command = new AssignRoleToUserCommand(Guid.Empty, "Dispatcher");

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
            var command = new AssignRoleToUserCommand(Guid.NewGuid(), "");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.RoleName));
        }
    }
}

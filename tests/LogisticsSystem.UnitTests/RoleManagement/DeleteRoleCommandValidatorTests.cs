using FluentAssertions;
using LogisticsSystem.Application.Features.RoleManagement.Commands.DeleteRole;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class DeleteRoleCommandValidatorTests
    {
        private readonly DeleteRoleCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenIdIsValid_ShouldPass()
        {
            // Arrange
            var command = new DeleteRoleCommand(Guid.NewGuid());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenIdIsEmpty_ShouldFail()
        {
            // Arrange
            var command = new DeleteRoleCommand(Guid.Empty);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
        }
    }
}

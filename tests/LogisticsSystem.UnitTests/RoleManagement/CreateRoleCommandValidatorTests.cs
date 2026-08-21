using FluentAssertions;
using LogisticsSystem.Application.Features.RoleManagement.Commands.CreateRole;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class CreateRoleCommandValidatorTests
    {
        private readonly CreateRoleCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenValidName_ShouldPass()
        {
            // Arrange
            var command = new CreateRoleCommand("Manager");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_WhenNameIsEmpty_ShouldFail(string name)
        {
            // Arrange
            var command = new CreateRoleCommand(name);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
        }
    }
}

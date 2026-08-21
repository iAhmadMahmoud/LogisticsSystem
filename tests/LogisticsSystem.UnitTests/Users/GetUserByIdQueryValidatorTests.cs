using FluentAssertions;
using LogisticsSystem.Application.Features.Users.Queries.GetUserById;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class GetUserByIdQueryValidatorTests
    {
        private readonly GetUserByIdQueryValidator _validator = new();

        [Fact]
        public void Validate_WhenIdIsValid_ShouldPass()
        {
            // Arrange
            var query = new GetUserByIdQuery(Guid.NewGuid());

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenIdIsEmpty_ShouldFail()
        {
            // Arrange
            var query = new GetUserByIdQuery(Guid.Empty);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(query.Id));
        }
    }
}

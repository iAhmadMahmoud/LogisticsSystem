using FluentAssertions;
using LogisticsSystem.Application.Features.Users.Queries.GetUsers;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class GetUsersQueryValidatorTests
    {
        private readonly GetUsersQueryValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPass()
        {
            // Arrange
            var query = new GetUsersQuery(1, 20);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WhenInvalidPageNumber_ShouldFail(int pageNumber)
        {
            // Arrange
            var query = new GetUsersQuery(pageNumber, 20);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(query.PageNumber));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(101)]
        public void Validate_WhenInvalidPageSize_ShouldFail(int pageSize)
        {
            // Arrange
            var query = new GetUsersQuery(1, pageSize);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(query.PageSize));
        }
    }
}

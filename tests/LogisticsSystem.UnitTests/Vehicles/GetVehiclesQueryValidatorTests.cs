using FluentAssertions;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicles;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class GetVehiclesQueryValidatorTests
    {
        private readonly GetVehiclesQueryValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPass()
        {
            // Arrange
            var query = new GetVehiclesQuery(1, 10);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WhenPageNumberLessThanOne_ShouldFail(int pageNumber)
        {
            // Arrange
            var query = new GetVehiclesQuery(pageNumber, 10);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(query.PageNumber));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_WhenPageSizeOutOfRange_ShouldFail(int pageSize)
        {
            // Arrange
            var query = new GetVehiclesQuery(1, pageSize);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(query.PageSize));
        }
    }
}

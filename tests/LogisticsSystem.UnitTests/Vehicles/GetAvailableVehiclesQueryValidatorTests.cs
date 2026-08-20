using FluentAssertions;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetAvailableVehicles;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class GetAvailableVehiclesQueryValidatorTests
    {
        private readonly GetAvailableVehiclesQueryValidator _validator = new();

        [Fact]
        public void Validate_WhenValidParameters_ShouldPassValidation()
        {
            // Arrange
            var query = new GetAvailableVehiclesQuery(PageNumber: 1, PageSize: 10);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WhenPageNumberLessThanOne_ShouldFailValidation(int pageNumber)
        {
            // Arrange
            var query = new GetAvailableVehiclesQuery(PageNumber: pageNumber, PageSize: 10);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(query.PageNumber));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(51)]
        [InlineData(100)]
        public void Validate_WhenPageSizeOutOfRange_ShouldFailValidation(int pageSize)
        {
            // Arrange
            var query = new GetAvailableVehiclesQuery(PageNumber: 1, PageSize: pageSize);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(query.PageSize));
        }
    }
}

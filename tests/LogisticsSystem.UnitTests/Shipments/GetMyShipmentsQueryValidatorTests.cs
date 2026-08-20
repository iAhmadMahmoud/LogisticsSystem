using FluentAssertions;
using LogisticsSystem.Application.Features.Shipments.Queries.GetMyShipments;
using Xunit;

namespace LogisticsSystem.UnitTests.Shipments
{
    public class GetMyShipmentsQueryValidatorTests
    {
        private readonly GetMyShipmentsQueryValidator _validator = new();

        [Fact]
        public void Validate_ValidQuery_PassesValidation()
        {
            // Arrange
            var query = new GetMyShipmentsQuery(PageNumber: 1, PageSize: 10);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidPageNumber_FailsValidation(int pageNumber)
        {
            // Arrange
            var query = new GetMyShipmentsQuery(PageNumber: pageNumber, PageSize: 10);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyShipmentsQuery.PageNumber));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        [InlineData(-5)]
        public void Validate_InvalidPageSize_FailsValidation(int pageSize)
        {
            // Arrange
            var query = new GetMyShipmentsQuery(PageNumber: 1, PageSize: pageSize);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyShipmentsQuery.PageSize));
        }
    }
}

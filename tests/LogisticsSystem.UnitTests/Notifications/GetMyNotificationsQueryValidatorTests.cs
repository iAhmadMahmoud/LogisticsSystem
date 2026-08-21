using FluentAssertions;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using Xunit;

namespace LogisticsSystem.UnitTests.Notifications
{
    public class GetMyNotificationsQueryValidatorTests
    {
        private readonly GetMyNotificationsQueryValidator _validator = new();

        [Theory]
        [InlineData(1, 10, true)]
        [InlineData(1, 100, true)]
        [InlineData(0, 10, false)]
        [InlineData(-1, 10, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 101, false)]
        public void Validate_ValidatesPageNumberAndPageSize(int pageNumber, int pageSize, bool isValid)
        {
            // Arrange
            var query = new GetMyNotificationsQuery(pageNumber, pageSize);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().Be(isValid);
        }
    }
}

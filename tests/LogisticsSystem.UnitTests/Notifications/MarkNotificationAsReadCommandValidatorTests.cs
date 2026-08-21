using FluentAssertions;
using LogisticsSystem.Application.Features.Notifications.Commands.MarkAsRead;
using Xunit;

namespace LogisticsSystem.UnitTests.Notifications
{
    public class MarkNotificationAsReadCommandValidatorTests
    {
        private readonly MarkNotificationAsReadCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenNotificationIdNotEmpty_IsValid()
        {
            var command = new MarkNotificationAsReadCommand(Guid.NewGuid());
            var result = _validator.Validate(command);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenNotificationIdIsEmpty_IsInvalid()
        {
            var command = new MarkNotificationAsReadCommand(Guid.Empty);
            var result = _validator.Validate(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "notificationId");
        }
    }
}

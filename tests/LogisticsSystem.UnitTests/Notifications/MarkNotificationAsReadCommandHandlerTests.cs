using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Notifications.Commands.MarkAsRead;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Notifications
{
    public class MarkNotificationAsReadCommandHandlerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly MarkNotificationAsReadCommandHandler _handler;

        public MarkNotificationAsReadCommandHandlerTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new MarkNotificationAsReadCommandHandler(_notificationServiceMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_CallsNotificationServiceMarkAsReadWithCurrentUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();
            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

            // Act
            await _handler.Handle(new MarkNotificationAsReadCommand(notificationId), CancellationToken.None);

            // Assert
            _notificationServiceMock.Verify(s => s.MarkAsReadAsync(notificationId, userId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

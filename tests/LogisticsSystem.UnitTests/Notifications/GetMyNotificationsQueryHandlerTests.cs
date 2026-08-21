using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Notifications
{
    public class GetMyNotificationsQueryHandlerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetMyNotificationsQueryHandler _handler;

        public GetMyNotificationsQueryHandlerTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new GetMyNotificationsQueryHandler(_notificationServiceMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_CallsNotificationServiceWithCurrentUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

            var pagedResult = new PagedResult<NotificationResponse>
            {
                Items = new List<NotificationResponse>
                {
                    new NotificationResponse(Guid.NewGuid(), "Title", "Msg", NotificationType.DispatchAssignmentReceived, false, null, DateTime.UtcNow)
                },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 1
            };

            _notificationServiceMock
                .Setup(s => s.GetMyNotificationAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(new GetMyNotificationsQuery(1, 10), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            _notificationServiceMock.Verify(s => s.GetMyNotificationAsync(userId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

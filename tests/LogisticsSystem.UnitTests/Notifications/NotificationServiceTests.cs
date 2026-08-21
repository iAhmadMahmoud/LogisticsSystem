using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Services;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Notifications
{
    public class NotificationServiceTests
    {
        private readonly Mock<IGenericRepository<Notification>> _notificationRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<INotificationRealtimeService> _realtimeMock;
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _notificationRepoMock = new Mock<IGenericRepository<Notification>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _realtimeMock = new Mock<INotificationRealtimeService>();

            _sut = new NotificationService(
                _notificationRepoMock.Object,
                _unitOfWorkMock.Object,
                _realtimeMock.Object);
        }

        [Fact]
        public async Task CreateAsync_AddsNotificationToRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            Notification? addedNotification = null;

            _notificationRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .Callback<Notification, CancellationToken>((n, _) => addedNotification = n)
                .Returns(Task.CompletedTask);

            // Act
            await _sut.CreateAsync(userId, "Test Title", "Test Message", NotificationType.DispatchAssignmentReceived, CancellationToken.None);

            // Assert
            _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
            addedNotification.Should().NotBeNull();
            addedNotification!.UserId.Should().Be(userId);
            addedNotification.Title.Should().Be("Test Title");
            addedNotification.Message.Should().Be("Test Message");
            addedNotification.Type.Should().Be(NotificationType.DispatchAssignmentReceived);
        }

        [Fact]
        public async Task GetMyNotificationAsync_ReturnsPagedNotifications()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = "N1",
                    Message = "M1",
                    Type = NotificationType.DispatchAssignmentReceived,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _notificationRepoMock
                .Setup(r => r.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _notificationRepoMock
                .Setup(r => r.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _sut.GetMyNotificationAsync(userId, 1, 10, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].Title.Should().Be("N1");
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotificationExistsAndUnread_MarksAsReadAndSaves()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = notificationId,
                UserId = userId,
                Title = "N1",
                Message = "M1",
                IsRead = false
            };

            _notificationRepoMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);

            // Act
            await _sut.MarkAsReadAsync(notificationId, userId, CancellationToken.None);

            // Assert
            notification.IsRead.Should().BeTrue();
            notification.ReadAt.Should().NotBeNull();
            _notificationRepoMock.Verify(r => r.Update(notification), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotificationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _notificationRepoMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Notification?)null);

            // Act
            var act = () => _sut.MarkAsReadAsync(notificationId, userId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenAlreadyRead_DoesNotUpdateOrSave()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = notificationId,
                UserId = userId,
                Title = "N1",
                Message = "M1",
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddMinutes(-5)
            };

            _notificationRepoMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);

            // Act
            await _sut.MarkAsReadAsync(notificationId, userId, CancellationToken.None);

            // Assert
            _notificationRepoMock.Verify(r => r.Update(It.IsAny<Notification>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendRealtimeAsync_CallsRealtimeService()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            await _sut.SendRealtimeAsync(userId, "Title", "Message", CancellationToken.None);

            // Assert
            _realtimeMock.Verify(r => r.SendAsync(userId, "Title", "Message", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

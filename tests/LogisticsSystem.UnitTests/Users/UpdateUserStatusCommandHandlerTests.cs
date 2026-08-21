using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.Users.Commands.UpdateUserStatus;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class UpdateUserStatusCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly UpdateUserStatusCommandHandler _handler;

        public UpdateUserStatusCommandHandlerTests()
        {
            _handler = new UpdateUserStatusCommandHandler(
                _identityServiceMock.Object,
                _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenAdminAttemptsToDeactivateSelf_ThrowsDomainException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var command = new UpdateUserStatusCommand(adminId, false);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Administrators cannot deactivate their own account.");
        }

        [Fact]
        public async Task Handle_WhenAdminUpdatesAnotherUserStatus_DelegatesToIdentityService()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var command = new UpdateUserStatusCommand(targetUserId, false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.SetUserStatusAsync(
                    targetUserId,
                    false,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

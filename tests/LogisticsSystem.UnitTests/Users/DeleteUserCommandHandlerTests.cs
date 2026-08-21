using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.Users.Commands.DeleteUser;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class DeleteUserCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly DeleteUserCommandHandler _handler;

        public DeleteUserCommandHandlerTests()
        {
            _handler = new DeleteUserCommandHandler(
                _identityServiceMock.Object,
                _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenAdminAttemptsToDeleteSelf_ThrowsDomainException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var command = new DeleteUserCommand(adminId);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Administrators cannot delete their own account.");
        }

        [Fact]
        public async Task Handle_WhenAdminDeletesAnotherUser_DelegatesToIdentityService()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var command = new DeleteUserCommand(targetUserId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.DeactivateOrDeleteUserAsync(
                    targetUserId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

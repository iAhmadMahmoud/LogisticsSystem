using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.Commands.RemoveRoleFromUser;
using LogisticsSystem.Application.Features.Users.Commands.DeleteUser;
using LogisticsSystem.Application.Features.Users.Commands.UpdateUserStatus;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Authorization
{
    public class AdminSelfProtectionTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task DeleteUser_WhenAdminAttemptsSelfDeletion_ThrowsDomainException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var handler = new DeleteUserCommandHandler(_identityServiceMock.Object, _currentUserServiceMock.Object);

            // Act
            var act = async () => await handler.Handle(new DeleteUserCommand(adminId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Administrators cannot delete their own account.");

            _identityServiceMock.Verify(x => x.DeactivateOrDeleteUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUser_WhenAdminDeletesOtherUser_Succeeds()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var handler = new DeleteUserCommandHandler(_identityServiceMock.Object, _currentUserServiceMock.Object);

            // Act
            await handler.Handle(new DeleteUserCommand(targetUserId), CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.DeactivateOrDeleteUserAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_WhenAdminAttemptsSelfDeactivation_ThrowsDomainException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var handler = new UpdateUserStatusCommandHandler(_identityServiceMock.Object, _currentUserServiceMock.Object);

            // Act
            var act = async () => await handler.Handle(new UpdateUserStatusCommand(adminId, false), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Administrators cannot deactivate their own account.");

            _identityServiceMock.Verify(x => x.SetUserStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserStatus_WhenAdminDeactivatesOtherUser_Succeeds()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var handler = new UpdateUserStatusCommandHandler(_identityServiceMock.Object, _currentUserServiceMock.Object);

            // Act
            await handler.Handle(new UpdateUserStatusCommand(targetUserId, false), CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.SetUserStatusAsync(targetUserId, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveRoleFromUser_WhenAdminAttemptsToRemoveAdminRoleFromSelf_ThrowsDomainException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var handler = new RemoveRoleFromUserCommandHandler(_identityServiceMock.Object, _currentUserServiceMock.Object);

            // Act
            var act = async () => await handler.Handle(new RemoveRoleFromUserCommand(adminId, Roles.Admin), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Administrators cannot remove the Admin role from their own account.");

            _identityServiceMock.Verify(x => x.RemoveRoleFromUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RemoveRoleFromUser_WhenAdminRemovesRoleFromOtherUser_Succeeds()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var handler = new RemoveRoleFromUserCommandHandler(_identityServiceMock.Object, _currentUserServiceMock.Object);

            // Act
            await handler.Handle(new RemoveRoleFromUserCommand(targetUserId, Roles.Dispatcher), CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.RemoveRoleFromUserAsync(targetUserId, Roles.Dispatcher, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

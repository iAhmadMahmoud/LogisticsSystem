using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.Commands.RemoveRoleFromUser;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Exceptions;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class RemoveRoleFromUserCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly RemoveRoleFromUserCommandHandler _handler;

        public RemoveRoleFromUserCommandHandlerTests()
        {
            _handler = new RemoveRoleFromUserCommandHandler(
                _identityServiceMock.Object,
                _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenAdminAttemptsToRemoveAdminRoleFromSelf_ThrowsDomainException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var command = new RemoveRoleFromUserCommand(adminId, Roles.Admin);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Administrators cannot remove the Admin role from their own account.");
        }

        [Fact]
        public async Task Handle_WhenRemovingNonAdminRoleFromSelf_DelegatesToIdentityService()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var command = new RemoveRoleFromUserCommand(adminId, Roles.Dispatcher);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.RemoveRoleFromUserAsync(adminId, Roles.Dispatcher, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenRemovingRoleFromAnotherUser_DelegatesToIdentityService()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

            var command = new RemoveRoleFromUserCommand(targetUserId, Roles.Dispatcher);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.RemoveRoleFromUserAsync(targetUserId, Roles.Dispatcher, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

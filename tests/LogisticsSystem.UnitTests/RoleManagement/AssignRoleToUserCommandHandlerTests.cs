using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.Commands.AssignRoleToUser;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class AssignRoleToUserCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly AssignRoleToUserCommandHandler _handler;

        public AssignRoleToUserCommandHandlerTests()
        {
            _handler = new AssignRoleToUserCommandHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCalled_DelegatesToIdentityService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleName = "Dispatcher";
            var command = new AssignRoleToUserCommand(userId, roleName);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.AssignRoleToUserAsync(userId, roleName, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.Commands.DeleteRole;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class DeleteRoleCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly DeleteRoleCommandHandler _handler;

        public DeleteRoleCommandHandlerTests()
        {
            _handler = new DeleteRoleCommandHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCalled_DelegatesToIdentityService()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var command = new DeleteRoleCommand(roleId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _identityServiceMock.Verify(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

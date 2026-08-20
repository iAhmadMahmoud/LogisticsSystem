using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using LogisticsSystem.Application.Features.RoleManagement.Commands.CreateRole;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class CreateRoleCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly CreateRoleCommandHandler _handler;

        public CreateRoleCommandHandlerTests()
        {
            _handler = new CreateRoleCommandHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCalled_DelegatesToIdentityServiceAndReturnsRoleDto()
        {
            // Arrange
            var command = new CreateRoleCommand("Auditor");
            var expectedDto = new RoleDto
            {
                Id = Guid.NewGuid(),
                Name = "Auditor",
                UserCount = 0,
                IsSystemRole = false
            };

            _identityServiceMock.Setup(x => x.CreateRoleAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedDto);
        }
    }
}

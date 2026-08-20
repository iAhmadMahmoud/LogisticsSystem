using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using LogisticsSystem.Application.Features.RoleManagement.Queries.GetRoles;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.RoleManagement
{
    public class GetRolesQueryHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly GetRolesQueryHandler _handler;

        public GetRolesQueryHandlerTests()
        {
            _handler = new GetRolesQueryHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCalled_ReturnsRolesFromIdentityService()
        {
            // Arrange
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto { Id = Guid.NewGuid(), Name = "Admin", UserCount = 1, IsSystemRole = true },
                new RoleDto { Id = Guid.NewGuid(), Name = "CustomRole", UserCount = 0, IsSystemRole = false }
            };

            _identityServiceMock.Setup(x => x.GetRolesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRoles);

            // Act
            var result = await _handler.Handle(new GetRolesQuery(), CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedRoles);
        }
    }
}

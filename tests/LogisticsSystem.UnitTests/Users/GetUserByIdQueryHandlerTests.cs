using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Application.Features.Users.Queries.GetUserById;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class GetUserByIdQueryHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly GetUserByIdQueryHandler _handler;

        public GetUserByIdQueryHandlerTests()
        {
            _handler = new GetUserByIdQueryHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenUserExists_ReturnsUserDetails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserByIdQuery(userId);
            var expectedUser = new UserDetailsDto
            {
                Id = userId,
                FirstName = "Jane",
                LastName = "Dispatcher",
                UserName = "janed",
                Email = "jane@logistics.com",
                IsActive = true,
                EmailConfirmed = true,
                Roles = ["Dispatcher"],
                CreatedAt = DateTime.UtcNow
            };

            _identityServiceMock.Setup(x => x.GetUserDetailsByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedUser);
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserByIdQuery(userId);

            _identityServiceMock.Setup(x => x.GetUserDetailsByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserDetailsDto?)null);

            // Act
            var act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }
    }
}

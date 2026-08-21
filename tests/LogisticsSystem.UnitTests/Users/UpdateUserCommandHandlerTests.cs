using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Application.Features.Users.Commands.UpdateUser;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class UpdateUserCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly UpdateUserCommandHandler _handler;

        public UpdateUserCommandHandlerTests()
        {
            _handler = new UpdateUserCommandHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCalled_DelegatesToIdentityServiceAndReturnsUpdatedUser()
        {
            // Arrange
            var command = new UpdateUserCommand(
                Guid.NewGuid(),
                "John",
                "Doe",
                "+1234567890",
                "john@logistics.com",
                "johndoe");

            var expectedResult = new UserDetailsDto
            {
                Id = command.Id,
                FirstName = command.FirstName,
                LastName = command.LastName,
                PhoneNumber = command.PhoneNumber,
                Email = command.Email,
                UserName = command.UserName,
                IsActive = true
            };

            _identityServiceMock.Setup(x => x.UpdateUserByAdminAsync(
                    command.Id,
                    command.FirstName,
                    command.LastName,
                    command.PhoneNumber,
                    command.Email,
                    command.UserName,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
        }
    }
}

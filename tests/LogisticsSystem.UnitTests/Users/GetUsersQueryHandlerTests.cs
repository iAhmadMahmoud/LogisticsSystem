using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Application.Features.Users.Queries.GetUsers;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Users
{
    public class GetUsersQueryHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly GetUsersQueryHandler _handler;

        public GetUsersQueryHandlerTests()
        {
            _handler = new GetUsersQueryHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCalled_ReturnsPagedResultFromIdentityService()
        {
            // Arrange
            var query = new GetUsersQuery(1, 10, "Admin", true, "john");
            var expectedResult = new PagedResult<UserDto>
            {
                Items =
                [
                    new UserDto
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "John",
                        LastName = "Admin",
                        UserName = "johnadmin",
                        Email = "john@admin.com",
                        IsActive = true,
                        Roles = ["Admin"],
                        CreatedAt = DateTime.UtcNow
                    }
                ],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };

            _identityServiceMock.Setup(x => x.GetUsersAsync(
                    query.PageNumber,
                    query.PageSize,
                    query.Role,
                    query.IsActive,
                    query.SearchTerm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
        }
    }
}

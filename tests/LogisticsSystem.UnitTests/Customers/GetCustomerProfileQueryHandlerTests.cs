using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Queries.GetCustomerProfile;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Domain.Entities;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Customers
{
    public class GetCustomerProfileQueryHandlerTests
    {
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly GetCustomerProfileQueryHandler _handler;

        public GetCustomerProfileQueryHandlerTests()
        {
            _handler = new GetCustomerProfileQueryHandler(
                _customerRepoMock.Object,
                _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCustomerExists_ReturnsCustomerProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = customerId,
                UserId = userId,
                DefaultAddress = "123 Main St"
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            var result = await _handler.Handle(new GetCustomerProfileQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(customerId);
            result.UserId.Should().Be(userId);
            result.DefaultAddress.Should().Be("123 Main St");
        }

        [Fact]
        public async Task Handle_WhenCustomerNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            // Act
            var act = async () => await _handler.Handle(new GetCustomerProfileQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Customer profile not found.");
        }
    }
}

using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Commands.UpdateCustomerProfile;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Domain.Entities;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Customers
{
    public class UpdateCustomerProfileCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<Customer>> _customerRepoMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IIdentityService> _identityServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly UpdateCustomerProfileCommandHandler _handler;

        public UpdateCustomerProfileCommandHandlerTests()
        {
            _handler = new UpdateCustomerProfileCommandHandler(
                _customerRepoMock.Object,
                _currentUserServiceMock.Object,
                _identityServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCustomerExists_UpdatesAddressAndIdentityProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = customerId,
                UserId = userId,
                DefaultAddress = "Old Address"
            };

            var command = new UpdateCustomerProfileCommand(
                "John",
                "Doe",
                "+1234567890",
                "New Address 456");

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            customer.DefaultAddress.Should().Be("New Address 456");
            _customerRepoMock.Verify(x => x.Update(customer), Times.Once);
            _identityServiceMock.Verify(x => x.UpdateProfileAsync(
                userId,
                "John",
                "Doe",
                "+1234567890",
                It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenCustomerNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UpdateCustomerProfileCommand("John", "Doe", null, null);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _customerRepoMock.Setup(x => x.FirstOrDefaultAsync(
                    It.IsAny<CustomerByUserIdSpecification>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Customer profile not found.");
        }
    }
}

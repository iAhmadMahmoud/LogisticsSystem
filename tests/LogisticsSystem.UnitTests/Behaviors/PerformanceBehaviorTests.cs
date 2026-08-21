using FluentAssertions;
using LogisticsSystem.Application.Common.Behaviors;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Behaviors
{
    public class PerformanceBehaviorTests
    {
        public record DummyFastRequest(string Data) : IRequest<string>;

        private readonly Mock<ILogger<PerformanceBehavior<DummyFastRequest, string>>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly PerformanceBehavior<DummyFastRequest, string> _behavior;

        public PerformanceBehaviorTests()
        {
            _loggerMock = new Mock<ILogger<PerformanceBehavior<DummyFastRequest, string>>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _behavior = new PerformanceBehavior<DummyFastRequest, string>(_loggerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_FastRequest_ReturnsResponseNormally()
        {
            // Arrange
            _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("QuickResult");

            // Act
            var result = await _behavior.Handle(new DummyFastRequest("fast"), nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be("QuickResult");
        }
    }
}

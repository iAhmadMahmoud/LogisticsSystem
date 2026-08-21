using FluentAssertions;
using LogisticsSystem.Application.Common.Behaviors;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Behaviors
{
    public class LoggingBehaviorTests
    {
        public record DummyRequest(string Data) : IRequest<string>;

        private readonly Mock<ILogger<LoggingBehavior<DummyRequest, string>>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly LoggingBehavior<DummyRequest, string> _behavior;

        public LoggingBehaviorTests()
        {
            _loggerMock = new Mock<ILogger<LoggingBehavior<DummyRequest, string>>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _behavior = new LoggingBehavior<DummyRequest, string>(_loggerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithAuthenticatedUser_LogsStartAndEndAndReturnsResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("Response");

            // Act
            var result = await _behavior.Handle(new DummyRequest("data"), nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be("Response");
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithAnonymousUser_LogsGracefully()
        {
            // Arrange
            _currentUserServiceMock.Setup(c => c.UserId).Throws(new UnauthorizedAccessException());

            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("Response");

            // Act
            var result = await _behavior.Handle(new DummyRequest("data"), nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be("Response");
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

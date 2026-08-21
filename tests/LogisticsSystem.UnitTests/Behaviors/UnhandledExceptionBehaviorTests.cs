using FluentAssertions;
using LogisticsSystem.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Behaviors
{
    public class UnhandledExceptionBehaviorTests
    {
        public record DummyFailingRequest(string Data) : IRequest<string>;

        [Fact]
        public async Task Handle_WhenExceptionThrown_LogsAndRethrows()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<UnhandledExceptionBehavior<DummyFailingRequest, string>>>();
            var behavior = new UnhandledExceptionBehavior<DummyFailingRequest, string>(loggerMock.Object);

            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Test Failure"));

            // Act
            var act = () => behavior.Handle(new DummyFailingRequest("fail"), nextMock.Object, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test Failure");
        }

        [Fact]
        public async Task Handle_WhenSuccessful_ReturnsResponse()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<UnhandledExceptionBehavior<DummyFailingRequest, string>>>();
            var behavior = new UnhandledExceptionBehavior<DummyFailingRequest, string>(loggerMock.Object);

            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("Success");

            // Act
            var result = await behavior.Handle(new DummyFailingRequest("pass"), nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be("Success");
        }
    }
}

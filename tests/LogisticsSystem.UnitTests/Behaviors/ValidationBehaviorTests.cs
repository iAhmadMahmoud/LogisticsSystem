using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LogisticsSystem.Application.Common.Behaviors;
using MediatR;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Behaviors
{
    public class ValidationBehaviorTests
    {
        public record DummyRequest(string Name) : IRequest<string>;

        [Fact]
        public async Task Handle_WhenNoValidators_CallsNext()
        {
            // Arrange
            var validators = Enumerable.Empty<IValidator<DummyRequest>>();
            var behavior = new ValidationBehavior<DummyRequest, string>(validators);
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("Success");

            // Act
            var result = await behavior.Handle(new DummyRequest("Valid"), nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be("Success");
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenValidationPasses_CallsNext()
        {
            // Arrange
            var validatorMock = new Mock<IValidator<DummyRequest>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DummyRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var behavior = new ValidationBehavior<DummyRequest, string>(new[] { validatorMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("Success");

            // Act
            var result = await behavior.Handle(new DummyRequest("Valid"), nextMock.Object, CancellationToken.None);

            // Assert
            result.Should().Be("Success");
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenValidationFails_ThrowsValidationException()
        {
            // Arrange
            var failure = new ValidationFailure("Name", "Name is required");
            var validatorMock = new Mock<IValidator<DummyRequest>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DummyRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { failure }));

            var behavior = new ValidationBehavior<DummyRequest, string>(new[] { validatorMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();

            // Act
            var act = () => behavior.Handle(new DummyRequest(""), nextMock.Object, CancellationToken.None);

            // Assert
            var ex = await act.Should().ThrowAsync<ValidationException>();
            ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required");
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

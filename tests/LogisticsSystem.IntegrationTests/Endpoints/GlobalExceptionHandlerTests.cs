using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LogisticsSystem.Api.Common.Extensions;
using LogisticsSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class GlobalExceptionHandlerTests
    {
        private readonly GlobalExceptionHandler _handler;

        public GlobalExceptionHandlerTests()
        {
            _handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        }

        [Fact]
        public async Task TryHandleAsync_ValidationException_Returns400BadRequestWithErrors()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;
            context.Request.Path = "/api/test";

            var validationFailures = new List<ValidationFailure>
            {
                new("PickupAddress", "Pickup address is required."),
                new("Weight", "Weight must be greater than zero.")
            };
            var exception = new ValidationException(validationFailures);

            // Act
            var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            handled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var doc = await JsonDocument.ParseAsync(responseBodyStream);
            var root = doc.RootElement;

            root.GetProperty("title").GetString().Should().Be("Validation Failed");
            root.GetProperty("status").GetInt32().Should().Be(400);
            root.GetProperty("detail").GetString().Should().Be("One or more validation errors occurred.");
            root.GetProperty("instance").GetString().Should().Be("/api/test");
            root.GetProperty("errors").TryGetProperty("PickupAddress", out _).Should().BeTrue();
            root.GetProperty("errors").TryGetProperty("Weight", out _).Should().BeTrue();
        }

        [Fact]
        public async Task TryHandleAsync_KeyNotFoundException_Returns404NotFoundProblemDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;
            context.Request.Path = "/api/Shipments/123";

            var exception = new KeyNotFoundException("Shipment not found.");

            // Act
            var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            handled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var doc = await JsonDocument.ParseAsync(responseBodyStream);
            var root = doc.RootElement;

            root.GetProperty("title").GetString().Should().Be("Resource Not Found");
            root.GetProperty("status").GetInt32().Should().Be(404);
            root.GetProperty("detail").GetString().Should().Be("Shipment not found.");
        }

        [Fact]
        public async Task TryHandleAsync_UnauthorizedAccessException_Returns403ForbiddenProblemDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;
            context.Request.Path = "/api/Shipments/456";

            var exception = new UnauthorizedAccessException("You are not authorized to view this shipment.");

            // Act
            var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            handled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var doc = await JsonDocument.ParseAsync(responseBodyStream);
            var root = doc.RootElement;

            root.GetProperty("title").GetString().Should().Be("Access Denied");
            root.GetProperty("status").GetInt32().Should().Be(403);
            root.GetProperty("detail").GetString().Should().Be("You are not authorized to view this shipment.");
        }

        [Fact]
        public async Task TryHandleAsync_DomainException_Returns422UnprocessableEntityProblemDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;
            context.Request.Path = "/api/Shipments/789/deliver";

            var exception = new DomainException("Cannot deliver shipment that is not in transit.");

            // Act
            var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            handled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var doc = await JsonDocument.ParseAsync(responseBodyStream);
            var root = doc.RootElement;

            root.GetProperty("title").GetString().Should().Be("Domain Rule Violated");
            root.GetProperty("status").GetInt32().Should().Be(422);
            root.GetProperty("detail").GetString().Should().Be("Cannot deliver shipment that is not in transit.");
        }

        [Fact]
        public async Task TryHandleAsync_InvalidOperationException_Returns409ConflictProblemDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;
            context.Request.Path = "/api/Auth/register";

            var exception = new InvalidOperationException("Email already exists.");

            // Act
            var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            handled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var doc = await JsonDocument.ParseAsync(responseBodyStream);
            var root = doc.RootElement;

            root.GetProperty("title").GetString().Should().Be("Business Rule Conflict");
            root.GetProperty("status").GetInt32().Should().Be(409);
            root.GetProperty("detail").GetString().Should().Be("Email already exists.");
        }

        [Fact]
        public async Task TryHandleAsync_GenericUnhandledException_Returns500InternalServerErrorProblemDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;
            context.Request.Path = "/api/Shipments";

            var exception = new TimeoutException("Database connection timed out.");

            // Act
            var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            handled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var doc = await JsonDocument.ParseAsync(responseBodyStream);
            var root = doc.RootElement;

            root.GetProperty("title").GetString().Should().Be("Server Error");
            root.GetProperty("status").GetInt32().Should().Be(500);
            root.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
        }
    }
}

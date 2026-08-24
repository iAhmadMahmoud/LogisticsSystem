using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class CorrelationIdIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CorrelationIdIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Request_WithoutCorrelationIdHeader_ReturnsResponseWithGeneratedCorrelationId()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health/live");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Contains("X-Correlation-ID").Should().BeTrue("Response must contain X-Correlation-ID header");
            var correlationId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            correlationId.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Request_WithCorrelationIdHeader_EchoesSameCorrelationId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var customId = "trace-" + Guid.NewGuid().ToString("N");

            var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
            request.Headers.Add("X-Correlation-ID", customId);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
            response.Headers.GetValues("X-Correlation-ID").First().Should().Be(customId);
        }

        [Fact]
        public async Task Request_WithErrorResponse_IncludesCorrelationIdInProblemDetails()
        {
            // Arrange
            var client = _factory.CreateClient();
            var customId = "trace-error-" + Guid.NewGuid().ToString("N");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/login")
            {
                Content = JsonContent.Create(new { Email = "invalid-email-format", Password = "" })
            };
            request.Headers.Add("X-Correlation-ID", customId);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Headers.Contains("X-Correlation-ID").Should().BeTrue();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.TryGetProperty("correlationId", out var corrProp).Should().BeTrue("ProblemDetails must include correlationId");
            corrProp.GetString().Should().Be(customId);
        }
    }
}

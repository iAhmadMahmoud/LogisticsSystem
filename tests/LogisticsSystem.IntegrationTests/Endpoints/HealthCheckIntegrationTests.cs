using System.Net;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class HealthCheckIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public HealthCheckIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetHealth_ReturnsOk_WithDetailedSanitizedJson()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.TryGetProperty("status", out var statusProp).Should().BeTrue();
            statusProp.GetString().Should().BeOneOf("Healthy", "Degraded");

            root.TryGetProperty("entries", out var entriesProp).Should().BeTrue();
            entriesProp.TryGetProperty("database", out var dbEntry).Should().BeTrue();
            dbEntry.GetProperty("status").GetString().Should().Be("Healthy");

            // Verify security: No secrets or stack traces in JSON
            content.Should().NotContain("Password=");
            content.Should().NotContain("SecretKey");
            content.Should().NotContain("StackTrace");
        }

        [Fact]
        public async Task GetHealthLive_ReturnsOk_WithProcessLivenessStatus()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health/live");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.GetProperty("status").GetString().Should().Be("Healthy");
        }

        [Fact]
        public async Task GetHealthReady_ReturnsOk_WithReadinessDependencies()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health/ready");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.TryGetProperty("entries", out var entriesProp).Should().BeTrue();
            entriesProp.TryGetProperty("database", out var dbEntry).Should().BeTrue();
            dbEntry.GetProperty("status").GetString().Should().Be("Healthy");
        }
    }
}

using System.Net;
using FluentAssertions;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class ProductionEnvironmentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ProductionEnvironmentIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task NonDevelopmentEnvironment_WhenSwaggerDisabled_ReturnsNotFoundForSwaggerUi()
        {
            // Arrange (Testing environment is non-Development)
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Swagger:EnabledInProduction"] = "false"
                    });
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Swagger UI should not be exposed in production when disabled");
        }

        [Fact]
        public async Task NonDevelopmentEnvironment_WhenSwaggerExplicitlyEnabled_ReturnsOk()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Swagger:EnabledInProduction"] = "true"
                    });
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, "Swagger UI should be accessible when explicitly enabled in production");
        }

        [Fact]
        public async Task ProductionConfiguration_HealthEndpoint_ReturnsHealthy()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}

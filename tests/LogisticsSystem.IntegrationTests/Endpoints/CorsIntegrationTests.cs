using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class CorsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CorsIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PreflightOptionsRequest_WithAllowedProductionOrigin_ReturnsCorsHeaders()
        {
            // Arrange
            var client = _factory.CreateClient();
            var origin = "https://app.logistics.com";

            var request = new HttpRequestMessage(HttpMethod.Options, "/api/Auth/login");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue("Allowed origin must receive Access-Control-Allow-Origin header");
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(origin);

            response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeTrue("CORS policy must allow credentials for authenticated SPA and SignalR");
            response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Contain("true");
        }

        [Fact]
        public async Task ActualRequest_WithAllowedOrigin_ReturnsCorsHeaders()
        {
            // Arrange
            var client = _factory.CreateClient();
            var origin = "https://admin.logistics.com";

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/login")
            {
                Content = JsonContent.Create(new
                {
                    Email = "admin@logistics.com",
                    Password = "SomePassword123!"
                })
            };
            request.Headers.Add("Origin", origin);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(origin);
            response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeTrue();
        }

        [Fact]
        public async Task Request_WithDisallowedOrigin_DoesNotReturnCorsAllowOriginHeader()
        {
            // Arrange
            var client = _factory.CreateClient();
            var untrustedOrigin = "https://untrusted-malicious-site.com";

            var request = new HttpRequestMessage(HttpMethod.Options, "/api/Auth/login");
            request.Headers.Add("Origin", untrustedOrigin);
            request.Headers.Add("Access-Control-Request-Method", "POST");

            // Act
            var response = await client.SendAsync(request);

            // Assert - Untrusted origins must NOT receive Access-Control-Allow-Origin header
            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse("Untrusted origin must be rejected by CORS policy");
        }

        [Fact]
        public async Task SignalRNegotiate_WithAllowedOrigin_ReturnsCorsHeaders()
        {
            // Arrange
            var client = _factory.CreateClient();
            var origin = "https://app.logistics.com";

            var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/notifications/negotiate?negotiateVersion=1");
            request.Headers.Add("Origin", origin);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue("SignalR negotiate must support allowed frontend origins");
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(origin);
            response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeTrue("SignalR requires credentials support");
        }
    }
}

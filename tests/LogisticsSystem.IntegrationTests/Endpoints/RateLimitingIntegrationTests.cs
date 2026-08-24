using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class RateLimitingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RateLimitingIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClientWithIp(string ipAddress)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
            return client;
        }

        [Fact]
        public async Task AuthEndpoint_BurstRequestsExceedingLimit_Returns429TooManyRequests()
        {
            // Arrange - use dedicated test IP
            var client = CreateClientWithIp("198.51.100.10");

            var loginPayload = new
            {
                Email = "ratelimit-test@example.com",
                Password = "WrongPassword123!"
            };

            // Auth limit is 5 requests per minute
            // Send 5 requests within permit limit
            for (int i = 0; i < 5; i++)
            {
                var response = await client.PostAsJsonAsync("/api/Auth/login", loginPayload);
                response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"request #{i + 1} should be within the permit limit");
            }

            // Act - 6th request exceeds the permit limit of 5
            var lastResponse = await client.PostAsJsonAsync("/api/Auth/login", loginPayload);

            // Assert
            lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "6th rapid request must be rejected with 429 Too Many Requests");

            // Verify Retry-After header
            lastResponse.Headers.Contains("Retry-After").Should().BeTrue("429 response must contain Retry-After header");

            // Verify RFC ProblemDetails body
            var content = await lastResponse.Content.ReadAsStringAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, TestAuthHelper.JsonOptions);

            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(429);
            problemDetails.Title.Should().Be("Too Many Requests");
            problemDetails.Detail.Should().Contain("Rate limit exceeded");
        }

        [Fact]
        public async Task LegitimateRequest_UnderLimit_IsNotBlockedByRateLimiter()
        {
            // Arrange - use distinct test IP
            var client = CreateClientWithIp("198.51.100.20");

            var registerPayload = new
            {
                FirstName = "Legit",
                LastName = "User",
                Username = "legituser",
                Email = "invalid-email-format",
                Password = "Password123!",
                PhoneNumber = "01000000000",
                Address = "Cairo, Egypt"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Auth/register", registerPayload);

            // Assert - Should reach controller validation (400/409), not rate limiter rejection (429)
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task TrackingEndpoint_MultipleTelematicsRequestsWithinQuota_AreAllowed()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services);
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id);
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, role: Roles.Customer);

            var client = CreateClientWithIp("198.51.100.30");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act - Send 10 rapid queries (well within the 120 tracking quota)
            for (int i = 0; i < 10; i++)
            {
                var response = await client.GetAsync($"/api/shipments/{shipment.Id}/tracking");
                response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
            }
        }
    }
}

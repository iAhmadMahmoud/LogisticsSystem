using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.Api.Contracts.Customers;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class CustomersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CustomersEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetMyProfile_WithoutToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/Customers/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMyProfile_WithValidToken_ReturnsCustomerProfile()
        {
            // Arrange
            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer_{Guid.NewGuid()}@test.com", address: "456 Oak Avenue");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.GetAsync("/api/Customers/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("456 Oak Avenue");
        }

        [Fact]
        public async Task UpdateMyProfile_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new UpdateCustomerProfileRequest("John", "Doe", "+1234567890", "123 Main St");

            // Act
            var response = await _client.PutAsJsonAsync("/api/Customers/me", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateMyProfile_WithValidToken_UpdatesProfileAndReturnsNoContent()
        {
            // Arrange
            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer_{Guid.NewGuid()}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            var request = new UpdateCustomerProfileRequest("Jane", "Smith", "+1987654321", "789 Pine Road");

            // Act
            var response = await client.PutAsJsonAsync("/api/Customers/me", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}

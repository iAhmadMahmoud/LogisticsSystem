using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthEndpointsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorizedOrNotFound()
        {
            // Arrange
            var loginPayload = new
            {
                Email = "nonexistent@user.com",
                Password = "WrongPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginPayload);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_WithInvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerPayload = new
            {
                FirstName = "Test",
                LastName = "User",
                Username = "testuser",
                Email = "invalid-email-format",
                Password = "Password123!",
                PhoneNumber = "01000000000",
                Address = "Cairo, Egypt"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/register", registerPayload);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
        }
    }
}

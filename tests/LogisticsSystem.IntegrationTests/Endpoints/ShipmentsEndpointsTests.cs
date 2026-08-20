using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class ShipmentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShipmentsEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateShipment_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var shipmentPayload = new
            {
                pickupAddress = "Address A",
                pickupLatitude = 30.0,
                pickupLongitude = 31.0,
                deliveryAddress = "Address B",
                deliveryLatitude = 30.1,
                deliveryLongitude = 31.1,
                weight = 10.0,
                distanceKm = 5.0,
                shippingCost = 100.0,
                priority = 0,
                scheduledAt = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Shipments", shipmentPayload);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetShipments_WithoutToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/Shipments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMyShipments_WithoutToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/Shipments/my-shipments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMyShipments_WithValidToken_ReturnsOnlyCustomerShipments()
        {
            // Arrange
            var (user1, customer1) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer1_{Guid.NewGuid()}@test.com");
            var (_, customer2) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer2_{Guid.NewGuid()}@test.com");

            var myShipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer1.Id, trackingNumber: "TRK-MY-MINE-01");
            var otherShipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer2.Id, trackingNumber: "TRK-OTHER-01");

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user1.Id);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.GetAsync("/api/Shipments/my-shipments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("TRK-MY-MINE-01");
            content.Should().NotContain("TRK-OTHER-01");
        }
    }
}

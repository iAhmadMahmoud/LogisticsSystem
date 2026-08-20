using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Hubs
{
    public class TrackingHubTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public TrackingHubTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HubConnection CreateHubConnection(string? token = null)
        {
            return new HubConnectionBuilder()
                .WithUrl(
                    new Uri(_factory.Server.BaseAddress, "/hubs/tracking"),
                    options =>
                    {
                        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                        if (!string.IsNullOrEmpty(token))
                        {
                            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                        }
                    })
                .Build();
        }

        [Fact]
        public async Task Connect_WithoutToken_FailsOrThrows()
        {
            // Arrange
            await using var connection = CreateHubConnection();

            // Act
            var act = async () => await connection.StartAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task Connect_WithValidCustomerToken_SuccessfullyConnects()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer_{Guid.NewGuid()}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token);

            // Act
            await connection.StartAsync();

            // Assert
            connection.State.Should().Be(HubConnectionState.Connected);

            await connection.StopAsync();
        }

        [Fact]
        public async Task SubscribeToShipment_WhenCustomerOwnsShipment_Succeeds()
        {
            // Arrange
            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer_{Guid.NewGuid()}@test.com");
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id);
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token);
            await connection.StartAsync();

            // Act
            var act = async () => await connection.InvokeAsync("SubscribeToShipment", shipment.Id);

            // Assert
            await act.Should().NotThrowAsync();

            await connection.StopAsync();
        }

        [Fact]
        public async Task SubscribeToShipment_WhenShipmentBelongsToOtherCustomer_ThrowsHubException()
        {
            // Arrange
            var (user1, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer1_{Guid.NewGuid()}@test.com");
            var (_, customer2) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer2_{Guid.NewGuid()}@test.com");
            var otherShipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer2.Id);

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user1.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token);
            await connection.StartAsync();

            // Act
            var act = async () => await connection.InvokeAsync("SubscribeToShipment", otherShipment.Id);

            // Assert
            await act.Should().ThrowAsync<HubException>()
                .WithMessage("*authorized to track this shipment*");

            await connection.StopAsync();
        }

        [Fact]
        public async Task LocationUpdated_WhenBroadcasted_ReceivedBySubscribedClient()
        {
            // Arrange
            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer_{Guid.NewGuid()}@test.com");
            var driverId = Guid.NewGuid();
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, driverId);
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token);

            var tcs = new TaskCompletionSource<LocationUpdatedDto>();
            connection.On<LocationUpdatedDto>("LocationUpdated", payload =>
            {
                tcs.TrySetResult(payload);
            });

            await connection.StartAsync();
            await connection.InvokeAsync("SubscribeToShipment", shipment.Id);

            // Act
            using var scope = _factory.Services.CreateScope();
            var realtimeService = scope.ServiceProvider.GetRequiredService<ITrackingRealtimeService>();
            await realtimeService.LocationUpdatedAsync(shipment.Id, driverId, 30.123, 31.456, DateTime.UtcNow);

            // Assert
            var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            result.ShipmentId.Should().Be(shipment.Id);
            result.DriverId.Should().Be(driverId);
            result.Latitude.Should().Be(30.123);
            result.Longitude.Should().Be(31.456);

            await connection.StopAsync();
        }

        [Fact]
        public async Task ShipmentStatusChanged_WhenBroadcasted_ReceivedBySubscribedClient()
        {
            // Arrange
            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"customer_{Guid.NewGuid()}@test.com");
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id);
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token);

            var tcs = new TaskCompletionSource<ShipmentStatusChangedDto>();
            connection.On<ShipmentStatusChangedDto>("ShipmentStatusChanged", payload =>
            {
                tcs.TrySetResult(payload);
            });

            await connection.StartAsync();
            await connection.InvokeAsync("SubscribeToShipment", shipment.Id);

            // Act
            using var scope = _factory.Services.CreateScope();
            var realtimeService = scope.ServiceProvider.GetRequiredService<ITrackingRealtimeService>();
            await realtimeService.ShipmentStatusChangedAsync(shipment.Id, ShipmentStatus.InTransit, DateTime.UtcNow, "On route");

            // Assert
            var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            result.ShipmentId.Should().Be(shipment.Id);
            result.Status.Should().Be(nameof(ShipmentStatus.InTransit));
            result.Notes.Should().Be("On route");

            await connection.StopAsync();
        }

        private sealed record LocationUpdatedDto(Guid ShipmentId, Guid DriverId, double Latitude, double Longitude, DateTime RecordedAt);
        private sealed record ShipmentStatusChangedDto(Guid ShipmentId, string Status, DateTime ChangedAt, string? Notes);
    }
}

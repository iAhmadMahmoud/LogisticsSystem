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

        [Fact]
        public async Task Connect_WithExpiredToken_FailsOrThrows()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"expired_{Guid.NewGuid()}@test.com");
            var expiredToken = TestAuthHelper.GenerateExpiredJwtToken(user.Id, user.Email!, Roles.Customer);

            await using var connection = CreateHubConnection(expiredToken);

            // Act
            var act = async () => await connection.StartAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task Connect_WithForgedToken_FailsOrThrows()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"forged_{Guid.NewGuid()}@test.com");
            var forgedToken = TestAuthHelper.GenerateForgedJwtToken(user.Id, user.Email!, Roles.Customer);

            await using var connection = CreateHubConnection(forgedToken);

            // Act
            var act = async () => await connection.StartAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task SubscribeToShipment_WhenDispatcherOrAdmin_Succeeds()
        {
            // Arrange
            var (_, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_sub_{Guid.NewGuid()}@test.com");
            var (dispUser, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"disp_sub_{Guid.NewGuid()}@test.com");
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id);

            var dispToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, dispUser.Id, role: Roles.Dispatcher);

            await using var connection = CreateHubConnection(dispToken);
            await connection.StartAsync();

            // Act - Dispatcher subscribes to customer's shipment
            var act = async () => await connection.InvokeAsync("SubscribeToShipment", shipment.Id);

            // Assert
            await act.Should().NotThrowAsync();

            await connection.StopAsync();
        }

        [Fact]
        public async Task LocationUpdated_WhenBroadcasted_NotReceivedByUnsubscribedClient()
        {
            // Arrange
            var (userA, customerA) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"userA_{Guid.NewGuid()}@test.com");
            var (userB, customerB) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"userB_{Guid.NewGuid()}@test.com");

            var driverId = Guid.NewGuid();
            var shipmentA = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customerA.Id, driverId);
            var shipmentB = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customerB.Id, driverId);

            var tokenA = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userA.Id, role: Roles.Customer);
            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userB.Id, role: Roles.Customer);

            await using var connA = CreateHubConnection(tokenA);
            await using var connB = CreateHubConnection(tokenB);

            var tcsA = new TaskCompletionSource<LocationUpdatedDto>();
            var receivedByB = false;

            connA.On<LocationUpdatedDto>("LocationUpdated", payload => tcsA.TrySetResult(payload));
            connB.On<LocationUpdatedDto>("LocationUpdated", _ => receivedByB = true);

            await connA.StartAsync();
            await connB.StartAsync();

            // Only user A subscribes to Shipment A
            await connA.InvokeAsync("SubscribeToShipment", shipmentA.Id);

            // Act - Broadcast location update for Shipment A
            using var scope = _factory.Services.CreateScope();
            var realtimeService = scope.ServiceProvider.GetRequiredService<ITrackingRealtimeService>();
            await realtimeService.LocationUpdatedAsync(shipmentA.Id, driverId, 29.987, 31.123, DateTime.UtcNow);

            // Assert
            var resultA = await tcsA.Task.WaitAsync(TimeSpan.FromSeconds(5));
            resultA.ShipmentId.Should().Be(shipmentA.Id);

            await Task.Delay(200);
            receivedByB.Should().BeFalse();

            await connA.StopAsync();
            await connB.StopAsync();
        }

        private sealed record LocationUpdatedDto(Guid ShipmentId, Guid DriverId, double Latitude, double Longitude, DateTime RecordedAt);
        private sealed record ShipmentStatusChangedDto(Guid ShipmentId, string Status, DateTime ChangedAt, string? Notes);
    }
}

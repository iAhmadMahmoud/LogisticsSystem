using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Hubs
{
    public class NotificationHubTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public NotificationHubTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HubConnection CreateHubConnection(string? token = null, bool useQueryString = false)
        {
            var uri = useQueryString && !string.IsNullOrEmpty(token)
                ? new Uri(_factory.Server.BaseAddress, $"/hubs/notifications?access_token={token}")
                : new Uri(_factory.Server.BaseAddress, "/hubs/notifications");

            return new HubConnectionBuilder()
                .WithUrl(
                    uri,
                    options =>
                    {
                        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                        if (!string.IsNullOrEmpty(token) && !useQueryString)
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
        public async Task Connect_WithValidBearerToken_SuccessfullyConnects()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_hub_bearer_{Guid.NewGuid()}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token);

            // Act
            await connection.StartAsync();

            // Assert
            connection.State.Should().Be(HubConnectionState.Connected);

            await connection.StopAsync();
        }

        [Fact]
        public async Task Connect_WithJwtQueryString_SuccessfullyConnects()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_hub_query_{Guid.NewGuid()}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token, useQueryString: true);

            // Act
            await connection.StartAsync();

            // Assert
            connection.State.Should().Be(HubConnectionState.Connected);

            await connection.StopAsync();
        }

        [Fact]
        public async Task NotificationSent_DeliveredToTargetUser_NotReceivedByOtherUser()
        {
            // Arrange
            var (userA, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_userA_{Guid.NewGuid()}@test.com");
            var (userB, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_userB_{Guid.NewGuid()}@test.com");

            var tokenA = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userA.Id, role: Roles.Customer);
            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userB.Id, role: Roles.Customer);

            await using var connA = CreateHubConnection(tokenA);
            await using var connB = CreateHubConnection(tokenB);

            var tcsA = new TaskCompletionSource<NotificationPayloadDto>();
            var receivedByB = false;

            connA.On<NotificationPayloadDto>("NotificationReceived", payload =>
            {
                tcsA.TrySetResult(payload);
            });

            connB.On<NotificationPayloadDto>("NotificationReceived", _ =>
            {
                receivedByB = true;
            });

            await connA.StartAsync();
            await connB.StartAsync();

            // Act - Send realtime notification to User A only
            using (var scope = _factory.Services.CreateScope())
            {
                var notifRealtime = scope.ServiceProvider.GetRequiredService<INotificationRealtimeService>();
                await notifRealtime.SendAsync(userA.Id, "Special Alert", "Welcome to the system User A!");
            }

            // Assert
            var resultA = await tcsA.Task.WaitAsync(TimeSpan.FromSeconds(5));
            resultA.Title.Should().Be("Special Alert");
            resultA.Message.Should().Be("Welcome to the system User A!");

            // Allow short time for any unwanted messages to arrive on B
            await Task.Delay(200);
            receivedByB.Should().BeFalse();

            await connA.StopAsync();
            await connB.StopAsync();
        }

        [Fact]
        public async Task Disconnect_HandledCorrectly()
        {
            // Arrange
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_disc_{Guid.NewGuid()}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, role: Roles.Customer);

            await using var connection = CreateHubConnection(token);
            await connection.StartAsync();
            connection.State.Should().Be(HubConnectionState.Connected);

            // Act
            await connection.StopAsync();

            // Assert
            connection.State.Should().Be(HubConnectionState.Disconnected);
        }

        private sealed record NotificationPayloadDto(string Title, string Message);
    }
}

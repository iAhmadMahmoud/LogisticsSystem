using System.Net;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class NotificationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public NotificationsEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetMyNotifications_WhenAuthenticated_ReturnsPagedNotificationsAndIsolatesUsers()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (userA, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_user_a_{Guid.NewGuid()}@test.com");
            var (userB, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_user_b_{Guid.NewGuid()}@test.com");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Notifications.AddRange(
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userA.Id,
                        Title = "User A Alert",
                        Message = "Alert 1",
                        Type = NotificationType.DispatchAssignmentReceived,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userB.Id,
                        Title = "User B Alert",
                        Message = "Alert 2",
                        Type = NotificationType.ShipmentAssigned,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                await db.SaveChangesAsync();
            }

            var tokenA = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userA.Id, userA.Email!, userA.UserName!, Roles.Customer);
            var clientA = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenA);

            // Act
            var response = await clientA.GetAsync("/api/Notifications?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResult<NotificationResponse>>(json, TestAuthHelper.JsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().ContainSingle(n => n.Title == "User A Alert");
            result.Items.Should().NotContain(n => n.Title == "User B Alert");
        }

        [Fact]
        public async Task GetMyNotifications_WhenAnonymous_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Notifications");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task MarkAsRead_WhenNotificationExists_MarksAsReadAndPersistsInDatabase()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_read_{Guid.NewGuid()}@test.com");

            var notificationId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Notifications.Add(new Notification
                {
                    Id = notificationId,
                    UserId = user.Id,
                    Title = "Unread Alert",
                    Message = "Please read",
                    Type = NotificationType.ShipmentDelivered,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, user.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act
            var response = await client.PatchAsync($"/api/Notifications/{notificationId}/read", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify persisted database state
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var persisted = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
                persisted.Should().NotBeNull();
                persisted!.IsRead.Should().BeTrue();
                persisted.ReadAt.Should().NotBeNull();
            }
        }
    }
}

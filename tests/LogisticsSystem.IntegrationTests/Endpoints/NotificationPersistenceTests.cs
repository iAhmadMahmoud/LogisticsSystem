using System.Net;
using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Services;
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
    public class NotificationPersistenceTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public NotificationPersistenceTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateAndPersistNotification_CorrectRecipientTitleMessageAndType()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_persist_{Guid.NewGuid()}@test.com");

            // Act
            using (var scope = _factory.Services.CreateScope())
            {
                var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notifService.CreateAsync(
                    user.Id,
                    "Order Dispatched",
                    "Your shipment is on the way.",
                    NotificationType.ShipmentInTransit);

                var unitOfWork = scope.ServiceProvider.GetRequiredService<LogisticsSystem.Application.Common.Interfaces.Persistence.IUnitOfWork>();
                await unitOfWork.SaveChangesAsync();
            }

            // Assert Database state
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notification = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == user.Id);

                notification.Should().NotBeNull();
                notification!.Title.Should().Be("Order Dispatched");
                notification.Message.Should().Be("Your shipment is on the way.");
                notification.Type.Should().Be(NotificationType.ShipmentInTransit);
                notification.IsRead.Should().BeFalse();
                notification.ReadAt.Should().BeNull();
                notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public async Task GetMyNotifications_ReturnsPersistedNotificationsAndStrictlyIsolatesOtherUsers()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (userA, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_iso_a_{Guid.NewGuid()}@test.com");
            var (userB, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_iso_b_{Guid.NewGuid()}@test.com");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Notifications.AddRange(
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userA.Id,
                        Title = "User A Only Alert",
                        Message = "Private Message for A",
                        Type = NotificationType.ShipmentPickedUp,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userB.Id,
                        Title = "User B Secret Alert",
                        Message = "Private Message for B",
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
            result!.Items.Should().ContainSingle(n => n.Title == "User A Only Alert");
            result.Items.Should().NotContain(n => n.Title == "User B Secret Alert");
        }

        [Fact]
        public async Task MarkAsRead_WhenUnread_UpdatesIsReadAndReadAt_Idempotently()
        {
            // Arrange
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notif_markread_{Guid.NewGuid()}@test.com");

            var notificationId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Notifications.Add(new Notification
                {
                    Id = notificationId,
                    UserId = user.Id,
                    Title = "Shipment Delivered",
                    Message = "Your package has arrived",
                    Type = NotificationType.ShipmentDelivered,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                });
                await db.SaveChangesAsync();
            }

            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, user.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Act 1 - Mark as read
            var res1 = await client.PatchAsync($"/api/Notifications/{notificationId}/read", null);
            res1.StatusCode.Should().Be(HttpStatusCode.NoContent);

            DateTime firstReadAt;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notif = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
                notif.Should().NotBeNull();
                notif!.IsRead.Should().BeTrue();
                notif.ReadAt.Should().NotBeNull();
                firstReadAt = notif.ReadAt!.Value;
            }

            // Act 2 - Mark as read again (idempotent)
            var res2 = await client.PatchAsync($"/api/Notifications/{notificationId}/read", null);
            res2.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notif = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
                notif.Should().NotBeNull();
                notif!.IsRead.Should().BeTrue();
                notif.ReadAt.Should().Be(firstReadAt);
            }
        }
    }
}

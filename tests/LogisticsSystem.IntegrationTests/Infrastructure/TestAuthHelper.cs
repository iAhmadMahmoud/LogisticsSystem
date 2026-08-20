using System.Net.Http.Headers;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsSystem.IntegrationTests.Infrastructure
{
    public static class TestAuthHelper
    {
        public static async Task<string> GenerateJwtTokenAsync(
            IServiceProvider services,
            Guid userId,
            string email = "test@logistics.com",
            string username = "testuser",
            string role = Roles.Customer)
        {
            var tokenGenerator = services.GetRequiredService<IJwtTokenGenerator>();
            var jwtUser = new JwtUser
            {
                Id = userId,
                UserName = username,
                Email = email,
                Roles = new List<string> { role }
            };
            return await tokenGenerator.GenerateAccessTokenAsync(jwtUser);
        }

        public static HttpClient CreateAuthenticatedClient(
            CustomWebApplicationFactory factory,
            string token)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public static async Task<(ApplicationUser User, Customer Customer)> SeedCustomerAsync(
            IServiceProvider services,
            string firstName = "John",
            string lastName = "Customer",
            string email = "customer@test.com",
            string address = "123 Customer Way")
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = "+1234567890",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                IsActive = true
            };

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DefaultAddress = address
            };

            db.Users.Add(user);
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            return (user, customer);
        }

        public static async Task<Shipment> SeedShipmentAsync(
            IServiceProvider services,
            Guid customerId,
            Guid? driverId = null,
            ShipmentStatus status = ShipmentStatus.InTransit,
            string trackingNumber = "TRK-TEST-001")
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                DriverId = driverId,
                TrackingNumber = trackingNumber,
                PickupAddress = "Pickup Location",
                DeliveryAddress = "Delivery Location",
                Weight = 10,
                DistanceKm = 5,
                ShippingCost = 50,
                Priority = ShipmentPriority.Normal,
                Status = status,
                ScheduledAt = DateTime.UtcNow.AddDays(1)
            };

            db.Shipments.Add(shipment);
            await db.SaveChangesAsync();

            return shipment;
        }
    }
}

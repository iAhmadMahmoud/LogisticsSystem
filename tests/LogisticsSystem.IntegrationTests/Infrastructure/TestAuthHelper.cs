using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LogisticsSystem.IntegrationTests.Infrastructure
{
    public static class TestAuthHelper
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task EnsureRolesSeededAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            string[] roles = [Roles.Admin, Roles.Customer, Roles.Driver, Roles.Dispatcher];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }
        }
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

        public static string GenerateExpiredJwtToken(Guid userId, string email = "test@logistics.com", string role = Roles.Customer)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TestSuperSecretKeyForIntegrationTests1234567890!"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Role, role)
            };
            var token = new JwtSecurityToken(
                issuer: "LogisticsSystem",
                audience: "LogisticsSystemUsers",
                claims: claims,
                notBefore: DateTime.UtcNow.AddHours(-2),
                expires: DateTime.UtcNow.AddHours(-1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateForgedJwtToken(Guid userId, string email = "test@logistics.com", string role = Roles.Customer)
        {
            var untrustedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("UntrustedForgedKeyWithDifferentSignature1234567890!"));
            var credentials = new SigningCredentials(untrustedKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Role, role)
            };
            var token = new JwtSecurityToken(
                issuer: "LogisticsSystem",
                audience: "LogisticsSystemUsers",
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
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
            string trackingNumber = "TRK-TEST-001",
            double pickupLatitude = 30.0,
            double pickupLongitude = 31.0)
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
                PickupLatitude = pickupLatitude,
                PickupLongitude = pickupLongitude,
                DeliveryAddress = "Delivery Location",
                DeliveryLatitude = 30.1,
                DeliveryLongitude = 31.1,
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

        public static async Task<(ApplicationUser User, Driver Driver)> SeedDriverAsync(
            IServiceProvider services,
            string firstName = "Bob",
            string lastName = "Driver",
            string email = "driver@test.com",
            string licenseNumber = "DL-TEST-12345",
            DriverStatus status = DriverStatus.Available,
            double latitude = 30.0,
            double longitude = 31.0)
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
                PhoneNumber = "+1987654321",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                IsActive = true
            };

            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                LicenseNumber = licenseNumber,
                Status = status,
                Latitude = latitude,
                Longitude = longitude
            };

            db.Users.Add(user);
            db.Drivers.Add(driver);
            await db.SaveChangesAsync();

            return (user, driver);
        }

        public static async Task<Vehicle> SeedVehicleAsync(
            IServiceProvider services,
            string plateNumber = "TEST-VEH-001",
            string brand = "Ford",
            string model = "Transit",
            int year = 2023,
            string color = "White",
            VehicleType type = VehicleType.Van,
            decimal capacity = 2000,
            bool isActive = true)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = plateNumber,
                Brand = brand,
                Model = model,
                ManufacturingYear = year,
                Color = color,
                Type = type,
                Capacity = capacity,
                IsActive = isActive
            };

            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();

            return vehicle;
        }
    }
}

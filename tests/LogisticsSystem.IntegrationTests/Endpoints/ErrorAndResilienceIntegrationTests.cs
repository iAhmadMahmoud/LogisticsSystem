using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LogisticsSystem.Application.Authentication.Commands.Register;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LogisticsSystem.IntegrationTests.Endpoints
{
    public class ErrorAndResilienceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ErrorAndResilienceIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string GenerateExpiredToken(string secretKey = "TestSuperSecretKeyForIntegrationTests1234567890!")
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Email, "expired@test.com"),
                new(JwtRegisteredClaimNames.UniqueName, "expireduser"),
                new(ClaimTypes.Role, Roles.Customer)
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

        [Fact]
        public async Task Authentication_MissingJwt_Returns401Unauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/Shipments");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Authentication_InvalidJwt_Returns401Unauthorized()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.malformed.token");

            var response = await client.GetAsync("/api/Shipments");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Authentication_ExpiredJwt_Returns401Unauthorized()
        {
            var expiredToken = GenerateExpiredToken();
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

            var response = await client.GetAsync("/api/Shipments");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Validation_InvalidRequest_Returns400BadRequestWithProblemDetails()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"val_err_{Guid.NewGuid():N}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, user.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            // Send empty / invalid shipment command
            var invalidCommand = new CreateShipmentCommand(new CreateShipmentDto
            {
                PickupAddress = "", // empty
                DeliveryAddress = "", // empty
                Weight = -5m, // negative
                DistanceKm = 0m,
                ShippingCost = -10m
            });

            var response = await client.PostAsJsonAsync("/api/Shipments", invalidCommand);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestAuthHelper.JsonOptions);
            problem.Should().NotBeNull();
            problem!.Title.Should().Be("Validation Failed");
            problem.Status.Should().Be(StatusCodes.Status400BadRequest);
            problem.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task NotFound_MissingEntity_Returns404NotFoundWithProblemDetails()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"notfound_{Guid.NewGuid():N}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, user.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            var nonExistentId = Guid.NewGuid();
            var response = await client.GetAsync($"/api/Shipments/{nonExistentId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestAuthHelper.JsonOptions);
            problem.Should().NotBeNull();
            problem!.Title.Should().Be("Resource Not Found");
            problem.Status.Should().Be(StatusCodes.Status404NotFound);
            problem.Detail.Should().Contain("Shipment not found.");
        }

        [Fact]
        public async Task Forbidden_CrossCustomerResourceAccess_Returns403ForbiddenWithProblemDetails()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            // Seed Customer A and their shipment
            var (userA, customerA) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_a_{Guid.NewGuid():N}@test.com");
            var shipmentA = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customerA.Id);

            // Seed Customer B
            var (userB, _) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_b_{Guid.NewGuid():N}@test.com");
            var tokenB = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, userB.Id, userB.Email!, userB.UserName!, Roles.Customer);
            var clientB = TestAuthHelper.CreateAuthenticatedClient(_factory, tokenB);

            // Customer B attempts to view status history of Customer A's shipment
            var response = await clientB.GetAsync($"/api/Shipments/{shipmentA.Id}/status-history");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestAuthHelper.JsonOptions);
            problem.Should().NotBeNull();
            problem!.Title.Should().Be("Access Denied");
            problem.Status.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task DomainRuleViolation_InvalidStateTransition_Returns422UnprocessableEntityWithProblemDetails()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var (custUser, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cust_dom_{Guid.NewGuid():N}@test.com");
            var (drvUser, driver) = await TestAuthHelper.SeedDriverAsync(_factory.Services, email: $"drv_dom_{Guid.NewGuid():N}@test.com");

            // Seed shipment in Pending status
            var shipment = await TestAuthHelper.SeedShipmentAsync(_factory.Services, customer.Id, driverId: driver.Id, status: ShipmentStatus.Pending);

            var drvToken = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, drvUser.Id, drvUser.Email!, drvUser.UserName!, Roles.Driver);
            var driverClient = TestAuthHelper.CreateAuthenticatedClient(_factory, drvToken);

            // Driver attempts to Deliver directly from Pending (invalid transition: Pending -> Delivered)
            var response = await driverClient.PostAsync($"/api/Shipments/{shipment.Id}/deliver", null);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestAuthHelper.JsonOptions);
            problem.Should().NotBeNull();
            problem!.Title.Should().Be("Domain Rule Violated");
            problem.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        }

        [Fact]
        public async Task Conflict_DuplicateEmailRegistration_Returns409ConflictWithProblemDetails()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);

            var existingEmail = $"conflict_{Guid.NewGuid():N}@test.com";
            await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: existingEmail);

            var anonymousClient = _factory.CreateClient();
            var registerRequest = new RegisterRequest
            {
                FirstName = "Duplicate",
                LastName = "User",
                Username = $"dup_{Guid.NewGuid():N}",
                Email = existingEmail,
                Password = "Password123!"
            };

            var response = await anonymousClient.PostAsJsonAsync("/api/Auth/register", registerRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestAuthHelper.JsonOptions);
            problem.Should().NotBeNull();
            problem!.Title.Should().Be("Business Rule Conflict");
            problem.Status.Should().Be(StatusCodes.Status409Conflict);
        }

        [Fact]
        public async Task SignalR_UnauthenticatedOrInvalidJwt_FailsConnection()
        {
            // 1. Unauthenticated connection
            var unauthConnection = new HubConnectionBuilder()
                .WithUrl(
                    new Uri(_factory.Server.BaseAddress, "/hubs/notifications"),
                    options =>
                    {
                        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    })
                .Build();

            Func<Task> connectUnauth = async () => await unauthConnection.StartAsync();
            await connectUnauth.Should().ThrowAsync<HttpRequestException>();

            // 2. Expired JWT connection
            var expiredToken = GenerateExpiredToken();
            var expiredConnection = new HubConnectionBuilder()
                .WithUrl(
                    new Uri(_factory.Server.BaseAddress, "/hubs/notifications"),
                    options =>
                    {
                        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                        options.AccessTokenProvider = () => Task.FromResult<string?>(expiredToken);
                    })
                .Build();

            Func<Task> connectExpired = async () => await expiredConnection.StartAsync();
            await connectExpired.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task CancellationToken_AbortedRequest_CancelsCleanlyWithoutCorruptingDatabase()
        {
            await TestAuthHelper.EnsureRolesSeededAsync(_factory.Services);
            var (user, customer) = await TestAuthHelper.SeedCustomerAsync(_factory.Services, email: $"cancel_{Guid.NewGuid():N}@test.com");
            var token = await TestAuthHelper.GenerateJwtTokenAsync(_factory.Services, user.Id, user.Email!, user.UserName!, Roles.Customer);
            var client = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // pre-cancel token

            var createCommand = new CreateShipmentCommand(new CreateShipmentDto
            {
                PickupAddress = "Cancelled St",
                DeliveryAddress = "Nowhere",
                Weight = 10,
                DistanceKm = 5,
                ShippingCost = 50,
                Priority = ShipmentPriority.Normal
            });

            // Sending request with already cancelled token
            Func<Task> act = async () => await client.PostAsJsonAsync("/api/Shipments", createCommand, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();

            // Verify no shipment was created in DB
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var count = await db.Shipments.CountAsync(s => s.CustomerId == customer.Id);
            count.Should().Be(0, "canceled request must not persist partial records to database");
        }

        [Fact]
        public async Task BackgroundJob_Resilience_HandlesEmptyOrIsolatedExecutionCleanly()
        {
            using var scope = _factory.Services.CreateScope();
            var expirationService = scope.ServiceProvider.GetRequiredService<IAssignmentExpirationService>();

            // Executing when no expired assignments exist should complete cleanly without throwing
            Func<Task> act = async () => await expirationService.ExpireAssignmentsAsync(CancellationToken.None);
            await act.Should().NotThrowAsync();
        }
    }
}

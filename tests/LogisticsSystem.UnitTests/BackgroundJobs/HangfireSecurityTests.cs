using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Hangfire;
using Hangfire.Dashboard;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.BackgroundJobs
{
    public class HangfireSecurityTests
    {
        private const string SecretKey = "TestSuperSecretKeyForHangfireSecurityTests1234567890!";
        private const string Issuer = "LogisticsSystem";
        private const string Audience = "LogisticsSystemUsers";

        private readonly HangfireAuthorizationFilter _filter = new();
        private readonly IOptions<JwtOptions> _jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = SecretKey,
            Issuer = Issuer,
            Audience = Audience,
            AccessTokenExpirationMinutes = 60
        });

        private static string GenerateTestToken(string role, DateTime? expires = null, string? signingKey = null)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, role)
            };

            var expiration = expires ?? DateTime.UtcNow.AddMinutes(30);
            var notBefore = expiration < DateTime.UtcNow ? expiration.AddMinutes(-10) : DateTime.UtcNow.AddMinutes(-5);

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                notBefore: notBefore,
                expires: expiration,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private DashboardContext CreateContext(HttpContext httpContext)
        {
            if (httpContext.RequestServices == null)
            {
                var services = new ServiceCollection();
                services.AddSingleton(_jwtOptions);
                httpContext.RequestServices = services.BuildServiceProvider();
            }

            var storageMock = new Mock<JobStorage>();
            return new AspNetCoreDashboardContext(storageMock.Object, new DashboardOptions(), httpContext);
        }

        [Fact]
        public void Authorize_WhenUserIsAnonymousAndNoToken_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IOptions<JwtOptions>))).Returns(_jwtOptions);
            httpContext.RequestServices = serviceProviderMock.Object;

            var context = CreateContext(httpContext);

            // Act
            var result = _filter.Authorize(context);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(Roles.Customer)]
        [InlineData(Roles.Driver)]
        [InlineData(Roles.Dispatcher)]
        public void Authorize_WhenUserIsNonAdminRole_ReturnsFalse(string role)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Bearer");
            httpContext.User = new ClaimsPrincipal(identity);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IOptions<JwtOptions>))).Returns(_jwtOptions);
            httpContext.RequestServices = serviceProviderMock.Object;

            var context = CreateContext(httpContext);

            // Act
            var result = _filter.Authorize(context);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Authorize_WhenUserIsAuthenticatedAdmin_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, Roles.Admin) }, "Bearer");
            httpContext.User = new ClaimsPrincipal(identity);

            var context = CreateContext(httpContext);

            // Act
            var result = _filter.Authorize(context);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Authorize_WhenValidAdminJwtInQueryString_ReturnsTrue()
        {
            // Arrange
            var adminToken = GenerateTestToken(Roles.Admin);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?access_token={adminToken}");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_jwtOptions);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var context = CreateContext(httpContext);

            // Act
            var result = _filter.Authorize(context);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Authorize_WhenNonAdminJwtInQueryString_ReturnsFalse()
        {
            // Arrange
            var customerToken = GenerateTestToken(Roles.Customer);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?access_token={customerToken}");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_jwtOptions);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var context = CreateContext(httpContext);

            // Act
            var result = _filter.Authorize(context);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Authorize_WhenExpiredAdminJwtInQueryString_ReturnsFalse()
        {
            // Arrange
            var expiredToken = GenerateTestToken(Roles.Admin, expires: DateTime.UtcNow.AddMinutes(-10));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?access_token={expiredToken}");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_jwtOptions);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var context = CreateContext(httpContext);

            // Act
            var result = _filter.Authorize(context);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Authorize_WhenForgedAdminJwtInQueryString_ReturnsFalse()
        {
            // Arrange
            var forgedToken = GenerateTestToken(Roles.Admin, signingKey: "DifferentUntrustedKeyForSignatureForging1234567890!");
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?access_token={forgedToken}");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_jwtOptions);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var context = CreateContext(httpContext);

            // Act
            var result = _filter.Authorize(context);

            // Assert
            result.Should().BeFalse();
        }
    }
}

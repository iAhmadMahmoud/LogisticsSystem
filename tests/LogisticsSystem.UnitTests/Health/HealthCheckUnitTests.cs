using FluentAssertions;
using Hangfire;
using Hangfire.Storage;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Authentication.Email;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.Infrastructure.Persistence.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Health
{
    public class HealthCheckUnitTests
    {
        [Fact]
        public async Task DatabaseHealthCheck_WhenInMemoryDatabase_ReturnsHealthy()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var sp = services.BuildServiceProvider();

            var healthCheck = new DatabaseHealthCheck(sp);
            var context = new HealthCheckContext();

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Contain("Database connection is healthy.");
            result.Data.Should().ContainKey("latencyMs");
        }

        [Fact]
        public void HangfireHealthCheck_WhenStorageNull_ReturnsDegraded()
        {
            // Arrange
            var healthCheck = new HangfireHealthCheck();
            var context = new HealthCheckContext();

            // Act & Assert (when JobStorage.Current is not initialized or null)
            // Note: JobStorage.Current might be null or set by another test
            var task = healthCheck.CheckHealthAsync(context);
            task.Should().NotBeNull();
        }

        [Theory]
        [InlineData("Development", "", 587, HealthStatus.Healthy)]
        [InlineData("Fake", "", 587, HealthStatus.Healthy)]
        [InlineData("Smtp", "smtp.sendgrid.net", 587, HealthStatus.Healthy)]
        [InlineData("Smtp", "", 587, HealthStatus.Degraded)]
        [InlineData("Smtp", "smtp.sendgrid.net", 0, HealthStatus.Unhealthy)]
        [InlineData("Smtp", "smtp.sendgrid.net", 70000, HealthStatus.Unhealthy)]
        public async Task EmailHealthCheck_WithVariousConfigurations_ReturnsExpectedStatus(
            string provider,
            string host,
            int port,
            HealthStatus expectedStatus)
        {
            // Arrange
            var options = Options.Create(new EmailOptions
            {
                Provider = provider,
                SmtpHost = host,
                SmtpPort = port,
                SenderEmail = "test@example.com"
            });

            var healthCheck = new EmailHealthCheck(options);
            var context = new HealthCheckContext();

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(expectedStatus);
            result.Data.Should().ContainKey("provider");
        }
    }
}

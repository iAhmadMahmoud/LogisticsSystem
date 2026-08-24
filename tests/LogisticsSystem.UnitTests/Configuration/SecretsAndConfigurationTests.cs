using System.Text.Json;
using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Infrastructure;
using LogisticsSystem.Infrastructure.Authentication.Email;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LogisticsSystem.UnitTests.Configuration
{
    public class SecretsAndConfigurationTests
    {
        private static string FindSolutionRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".gitignore")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find solution root directory.");
        }

        [Fact]
        public void AppSettings_ProductionTemplate_MustNotContainPlaintextSecrets()
        {
            // Read src/LogisticsSystem.Api/appsettings.json
            var solutionRoot = FindSolutionRoot();
            var appSettingsPath = Path.Combine(solutionRoot, "src", "LogisticsSystem.Api", "appsettings.json");
            File.Exists(appSettingsPath).Should().BeTrue("appsettings.json must exist in Api project");

            var jsonContent = File.ReadAllText(appSettingsPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Connection string must be empty in base template
            root.GetProperty("ConnectionStrings").GetProperty("LogisticsSystem").GetString()
                .Should().BeEmpty("ConnectionStrings:LogisticsSystem must not contain hardcoded credentials in appsettings.json");

            // Jwt SecretKey must be empty in base template
            root.GetProperty("Jwt").GetProperty("SecretKey").GetString()
                .Should().BeEmpty("Jwt:SecretKey must not contain hardcoded secret keys in appsettings.json");

            // Email SmtpPassword must be empty in base template
            root.GetProperty("Email").GetProperty("SmtpPassword").GetString()
                .Should().BeEmpty("Email:SmtpPassword must not contain hardcoded credentials in appsettings.json");
        }

        [Fact]
        public void ConfigurationBinding_WithEnvironmentVariables_BindsOptionsCorrectly()
        {
            // Simulate environment variables mapped by ASP.NET Core
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:LogisticsSystem"] = "Server=prod-sql.database.windows.net;Database=Logistics_Prod;User Id=dbadmin;Password=P@ssw0rdProd!123;",
                ["Jwt:Issuer"] = "LogisticsProdIssuer",
                ["Jwt:Audience"] = "LogisticsProdAudience",
                ["Jwt:SecretKey"] = "SuperSecretCryptographicallySecureProductionKey9876543210!",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "14",
                ["Email:Provider"] = "Smtp",
                ["Email:SenderEmail"] = "prod-no-reply@logistics.com",
                ["Email:SenderName"] = "Logistics Prod System",
                ["Email:SmtpHost"] = "smtp.sendgrid.net",
                ["Email:SmtpPort"] = "587",
                ["Email:SmtpUser"] = "apikey",
                ["Email:SmtpPassword"] = "SG.production_api_key_12345",
                ["Email:EnableSsl"] = "true",
                ["Email:ConfirmationUrl"] = "https://app.logistics.com/confirm-email",
                ["Email:ResetPasswordUrl"] = "https://app.logistics.com/reset-password",
                ["Dispatch:AssignmentExpirationMinutes"] = "10"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);

            var sp = services.BuildServiceProvider();

            // Verify JwtOptions
            var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
            jwtOptions.Issuer.Should().Be("LogisticsProdIssuer");
            jwtOptions.Audience.Should().Be("LogisticsProdAudience");
            jwtOptions.SecretKey.Should().Be("SuperSecretCryptographicallySecureProductionKey9876543210!");
            jwtOptions.AccessTokenExpirationMinutes.Should().Be(15);
            jwtOptions.RefreshTokenExpirationDays.Should().Be(14);

            // Verify EmailOptions
            var emailOptions = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            emailOptions.Provider.Should().Be("Smtp");
            emailOptions.SenderEmail.Should().Be("prod-no-reply@logistics.com");
            emailOptions.SenderName.Should().Be("Logistics Prod System");
            emailOptions.SmtpHost.Should().Be("smtp.sendgrid.net");
            emailOptions.SmtpPort.Should().Be(587);
            emailOptions.SmtpUser.Should().Be("apikey");
            emailOptions.SmtpPassword.Should().Be("SG.production_api_key_12345");
            emailOptions.EnableSsl.Should().BeTrue();
            emailOptions.ConfirmationUrl.Should().Be("https://app.logistics.com/confirm-email");
            emailOptions.ResetPasswordUrl.Should().Be("https://app.logistics.com/reset-password");

            // Verify IEmailSender resolved to SmtpEmailSender
            var emailSender = sp.GetRequiredService<IEmailSender>();
            emailSender.Should().BeOfType<SmtpEmailSender>();

            // Verify DispatchOptions
            var dispatchOptions = sp.GetRequiredService<IOptions<DispatchOptions>>().Value;
            dispatchOptions.AssignmentExpirationMinutes.Should().Be(10);
        }

        [Fact]
        public async Task JwtTokenGenerator_WithInjectedSecret_GeneratesValidToken()
        {
            var options = Options.Create(new JwtOptions
            {
                Issuer = "ProductionIssuer",
                Audience = "ProductionAudience",
                SecretKey = "ProductionSecretKey_MustBeLongEnoughForHmacSha256_1234567890!",
                AccessTokenExpirationMinutes = 30,
                RefreshTokenExpirationDays = 7
            });

            var generator = new JwtTokenGenerator(options);
            var user = new JwtUser
            {
                Id = Guid.NewGuid(),
                UserName = "production_admin",
                Email = "admin@company.com",
                Roles = new List<string> { "Admin" }
            };

            var token = await generator.GenerateAccessTokenAsync(user);

            token.Should().NotBeNullOrWhiteSpace();
            token.Split('.').Length.Should().Be(3, "JWT must contain header, payload, and signature");
        }

        [Fact]
        public void GitIgnore_MustContainEnvironmentAndSecretPatterns()
        {
            var solutionRoot = FindSolutionRoot();
            var gitIgnorePath = Path.Combine(solutionRoot, ".gitignore");
            File.Exists(gitIgnorePath).Should().BeTrue(".gitignore must exist at repository root");

            var gitIgnoreContent = File.ReadAllText(gitIgnorePath);

            gitIgnoreContent.Should().Contain(".env", "Must ignore .env files");
            gitIgnoreContent.Should().Contain("appsettings*.local.json", "Must ignore local appsettings overrides");
            gitIgnoreContent.Should().Contain("secrets.json", "Must ignore secrets.json");
            gitIgnoreContent.Should().Contain("*.pem", "Must ignore pem certificates");
            gitIgnoreContent.Should().Contain("*.key", "Must ignore key files");
        }
    }
}

using FluentAssertions;
using LogisticsSystem.Infrastructure.Authentication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Email
{
    public class EmailProviderTests
    {
        [Fact]
        public void EmailTemplateGenerator_ConfirmationEmail_RendersValidHtmlWithRecipientAndLink()
        {
            // Arrange
            var recipient = "john.doe@example.com";
            var url = "https://app.logistics.com/confirm-email?userId=123&token=abc";

            // Act
            var html = EmailTemplateGenerator.GenerateConfirmationEmailHtml(recipient, url);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("john.doe@example.com");
            html.Should().Contain("https://app.logistics.com/confirm-email?userId=123&amp;token=abc");
            html.Should().Contain("Confirm Email Address");
        }

        [Fact]
        public void EmailTemplateGenerator_PasswordResetEmail_RendersValidHtmlWithRecipientAndLink()
        {
            // Arrange
            var recipient = "jane.smith@example.com";
            var url = "https://app.logistics.com/reset-password?userId=456&token=xyz";

            // Act
            var html = EmailTemplateGenerator.GeneratePasswordResetEmailHtml(recipient, url);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("jane.smith@example.com");
            html.Should().Contain("https://app.logistics.com/reset-password?userId=456&amp;token=xyz");
            html.Should().Contain("Reset Password");
        }

        [Fact]
        public async Task FakeEmailSender_WhenInvoked_CompletesSuccessfully()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<FakeEmailSender>>();
            var sender = new FakeEmailSender(loggerMock.Object);

            // Act
            var act = () => sender.SendEmailAsync("customer@example.com", "Test Subject", "<p>Hello</p>");

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SmtpEmailSender_WhenHostEmpty_SuppressesDeliveryWithoutError()
        {
            // Arrange
            var options = Options.Create(new EmailOptions
            {
                Provider = "Smtp",
                SmtpHost = ""
            });
            var loggerMock = new Mock<ILogger<SmtpEmailSender>>();
            var sender = new SmtpEmailSender(options, loggerMock.Object);

            // Act
            var act = () => sender.SendEmailAsync("recipient@example.com", "Subject", "<p>Body</p>");

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Theory]
        [InlineData("", "Subject")]
        [InlineData("   ", "Subject")]
        [InlineData("valid@email.com", "")]
        [InlineData("valid@email.com", "   ")]
        public async Task SmtpEmailSender_WhenInvalidParameters_ThrowsArgumentException(string to, string subject)
        {
            // Arrange
            var options = Options.Create(new EmailOptions
            {
                Provider = "Smtp",
                SmtpHost = "smtp.example.com"
            });
            var loggerMock = new Mock<ILogger<SmtpEmailSender>>();
            var sender = new SmtpEmailSender(options, loggerMock.Object);

            // Act
            var act = () => sender.SendEmailAsync(to, subject, "<p>Body</p>");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SmtpEmailSender_WhenConnectionFails_ThrowsInvalidOperationExceptionAfterRetries()
        {
            // Arrange
            var options = Options.Create(new EmailOptions
            {
                Provider = "Smtp",
                SmtpHost = "127.0.0.1",
                SmtpPort = 65530, // Unused port to trigger fast connection failure
                MaxRetries = 2
            });
            var loggerMock = new Mock<ILogger<SmtpEmailSender>>();
            var sender = new SmtpEmailSender(options, loggerMock.Object);

            // Act
            var act = () => sender.SendEmailAsync("recipient@example.com", "Subject", "<p>Body</p>");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Failed to send email to recipient*");
        }
    }
}

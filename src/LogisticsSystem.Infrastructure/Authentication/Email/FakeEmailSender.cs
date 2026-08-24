using LogisticsSystem.Application.Common.Interfaces.Authentication;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.Authentication.Email
{
    public sealed class FakeEmailSender : IEmailSender
    {
        private readonly ILogger<FakeEmailSender> _logger;

        public FakeEmailSender(ILogger<FakeEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                """
                ---------------- DEVELOPMENT EMAIL ----------------
                To: {To}
                Subject: {Subject}

                {Body}

                ---------------------------------------------------
                """,
                to,
                subject,
                htmlBody);

            return Task.CompletedTask;
        }
    }
}

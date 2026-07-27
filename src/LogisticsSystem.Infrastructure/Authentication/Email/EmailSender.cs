using LogisticsSystem.Application.Common.Interfaces.Authentication;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.Authentication.Email
{
    public sealed class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
            """
            ---------------- EMAIL ----------------
            To: {To}
            Subject: {Subject}

            {Body}

            ---------------------------------------
            """,
            to,
            subject,
            htmlBody);

            return Task.CompletedTask;
        }
    }
}

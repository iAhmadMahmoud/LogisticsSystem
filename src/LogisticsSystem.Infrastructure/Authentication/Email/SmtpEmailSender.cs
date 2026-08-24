using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogisticsSystem.Infrastructure.Authentication.Email
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<EmailOptions> options,
            ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(to);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            {
                _logger.LogWarning("SMTP host is not configured. Email to {Recipient} with subject '{Subject}' was suppressed.", to, subject);
                return;
            }

            var maxRetries = Math.Max(1, _options.MaxRetries);
            var attempt = 0;
            var stopwatch = Stopwatch.StartNew();

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    _logger.LogInformation(
                        "Attempting to send email to {Recipient} (Subject: '{Subject}') via SMTP {Host}:{Port} (Attempt {Attempt}/{MaxAttempts}).",
                        to,
                        subject,
                        _options.SmtpHost,
                        _options.SmtpPort,
                        attempt,
                        maxRetries);

                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(
                            string.IsNullOrWhiteSpace(_options.SenderEmail) ? "no-reply@logistics.com" : _options.SenderEmail,
                            string.IsNullOrWhiteSpace(_options.SenderName) ? "Logistics System" : _options.SenderName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(new MailAddress(to));

                    using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
                    {
                        EnableSsl = _options.EnableSsl,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false
                    };

                    if (!string.IsNullOrWhiteSpace(_options.SmtpUser) && !string.IsNullOrWhiteSpace(_options.SmtpPassword))
                    {
                        smtpClient.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);
                    }

                    await smtpClient.SendMailAsync(mailMessage, cancellationToken);

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "Email successfully delivered to {Recipient} (Subject: '{Subject}') in {ElapsedMilliseconds} ms.",
                        to,
                        subject,
                        stopwatch.ElapsedMilliseconds);

                    return;
                }
                catch (Exception ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    _logger.LogWarning(
                        ex,
                        "Transient error sending email to {Recipient} via SMTP {Host}:{Port} (Attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s...",
                        to,
                        _options.SmtpHost,
                        _options.SmtpPort,
                        attempt,
                        maxRetries,
                        backoffDelay.TotalSeconds);

                    await Task.Delay(backoffDelay, cancellationToken);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        ex,
                        "Failed to deliver email to {Recipient} (Subject: '{Subject}') via SMTP {Host}:{Port} after {Attempt} attempt(s) ({ElapsedMilliseconds} ms).",
                        to,
                        subject,
                        _options.SmtpHost,
                        _options.SmtpPort,
                        attempt,
                        stopwatch.ElapsedMilliseconds);

                    throw new InvalidOperationException($"Failed to send email to recipient '{to}' via SMTP: {ex.Message}", ex);
                }
            }
        }
    }
}

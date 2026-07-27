namespace LogisticsSystem.Application.Common.Interfaces.Authentication
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}

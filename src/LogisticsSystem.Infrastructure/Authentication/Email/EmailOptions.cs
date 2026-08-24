using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Infrastructure.Authentication.Email
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        public string Provider { get; set; } = "Development";
        public string SenderEmail { get; set; } = "no-reply@logistics.com";
        public string SenderName { get; set; } = "Logistics System";
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public int MaxRetries { get; set; } = 3;
        public string ConfirmationUrl { get; set; } = string.Empty;
        public string ResetPasswordUrl { get; set; } = string.Empty;
    }
}

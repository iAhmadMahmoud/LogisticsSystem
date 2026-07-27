using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Infrastructure.Authentication.Email
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        public string ConfirmationUrl { get; set; } = string.Empty;
        public string ResetPasswordUrl { get; set; } = string.Empty;
    }
}

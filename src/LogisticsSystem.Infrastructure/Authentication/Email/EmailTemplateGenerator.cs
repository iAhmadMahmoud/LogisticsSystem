using System.Net;

namespace LogisticsSystem.Infrastructure.Authentication.Email
{
    public static class EmailTemplateGenerator
    {
        public static string GenerateConfirmationEmailHtml(string recipientName, string confirmationUrl)
        {
            var encodedName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(recipientName) ? "User" : recipientName);
            var encodedUrl = WebUtility.HtmlEncode(confirmationUrl);

            return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Confirm Your Email - Logistics System</title>
                <style>
                    body { margin: 0; padding: 0; background-color: #0f172a; font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #334155; }
                    .wrapper { width: 100%; table-layout: fixed; background-color: #0f172a; padding: 40px 0; }
                    .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2); }
                    .header { background: linear-gradient(135deg, #1e293b, #0f172a); padding: 32px; text-align: center; border-bottom: 2px solid #3b82f6; }
                    .header h1 { margin: 0; color: #ffffff; font-size: 24px; font-weight: 700; letter-spacing: 0.5px; }
                    .content { padding: 40px 32px; }
                    .greeting { font-size: 18px; font-weight: 600; color: #0f172a; margin-bottom: 16px; }
                    .message { font-size: 15px; line-height: 1.6; color: #475569; margin-bottom: 28px; }
                    .cta-box { text-align: center; margin: 32px 0; }
                    .btn { display: inline-block; background: linear-gradient(135deg, #2563eb, #1d4ed8); color: #ffffff !important; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-weight: 600; font-size: 15px; letter-spacing: 0.3px; box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3); }
                    .alt-link { font-size: 13px; color: #64748b; word-break: break-all; margin-top: 24px; line-height: 1.5; }
                    .footer { background-color: #f8fafc; padding: 24px 32px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }
                </style>
            </head>
            <body>
                <div class="wrapper">
                    <div class="container">
                        <div class="header">
                            <h1>Logistics System</h1>
                        </div>
                        <div class="content">
                            <div class="greeting">Welcome, {{encodedName}}!</div>
                            <div class="message">
                                Thank you for creating an account with Logistics System. To complete your registration and activate your account, please verify your email address.
                            </div>
                            <div class="cta-box">
                                <a href="{{encodedUrl}}" class="btn" target="_blank">Confirm Email Address</a>
                            </div>
                            <div class="alt-link">
                                If the button above doesn't work, copy and paste this link into your browser:<br />
                                <a href="{{encodedUrl}}" style="color: #2563eb;">{{encodedUrl}}</a>
                            </div>
                        </div>
                        <div class="footer">
                            &copy; {{DateTime.UtcNow.Year}} Logistics System. If you did not create this account, please ignore this email.
                        </div>
                    </div>
                </div>
            </body>
            </html>
            """;
        }

        public static string GeneratePasswordResetEmailHtml(string recipientName, string resetUrl)
        {
            var encodedName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(recipientName) ? "User" : recipientName);
            var encodedUrl = WebUtility.HtmlEncode(resetUrl);

            return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Reset Your Password - Logistics System</title>
                <style>
                    body { margin: 0; padding: 0; background-color: #0f172a; font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #334155; }
                    .wrapper { width: 100%; table-layout: fixed; background-color: #0f172a; padding: 40px 0; }
                    .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2); }
                    .header { background: linear-gradient(135deg, #1e293b, #0f172a); padding: 32px; text-align: center; border-bottom: 2px solid #ef4444; }
                    .header h1 { margin: 0; color: #ffffff; font-size: 24px; font-weight: 700; letter-spacing: 0.5px; }
                    .content { padding: 40px 32px; }
                    .greeting { font-size: 18px; font-weight: 600; color: #0f172a; margin-bottom: 16px; }
                    .message { font-size: 15px; line-height: 1.6; color: #475569; margin-bottom: 28px; }
                    .cta-box { text-align: center; margin: 32px 0; }
                    .btn { display: inline-block; background: linear-gradient(135deg, #dc2626, #b91c1c); color: #ffffff !important; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-weight: 600; font-size: 15px; letter-spacing: 0.3px; box-shadow: 0 4px 12px rgba(220, 38, 38, 0.3); }
                    .alt-link { font-size: 13px; color: #64748b; word-break: break-all; margin-top: 24px; line-height: 1.5; }
                    .footer { background-color: #f8fafc; padding: 24px 32px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }
                </style>
            </head>
            <body>
                <div class="wrapper">
                    <div class="container">
                        <div class="header">
                            <h1>Logistics System</h1>
                        </div>
                        <div class="content">
                            <div class="greeting">Hello, {{encodedName}}</div>
                            <div class="message">
                                We received a request to reset the password for your Logistics System account. Click the button below to choose a new secure password.
                            </div>
                            <div class="cta-box">
                                <a href="{{encodedUrl}}" class="btn" target="_blank">Reset Password</a>
                            </div>
                            <div class="alt-link">
                                If you did not request a password reset, you can safely ignore this email. This link will expire shortly for security purposes.<br /><br />
                                Direct link: <a href="{{encodedUrl}}" style="color: #dc2626;">{{encodedUrl}}</a>
                            </div>
                        </div>
                        <div class="footer">
                            &copy; {{DateTime.UtcNow.Year}} Logistics System. All rights reserved.
                        </div>
                    </div>
                </div>
            </body>
            </html>
            """;
        }
    }
}

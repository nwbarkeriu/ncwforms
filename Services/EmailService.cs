/*
 * EmailService - Dedicated service for handling email opera            var css = @"
                <style>
                body, table, th, td { font-family: Calibri, Arial, sans-serif; }
                thead, th { background-color: #73B6AD; }
                td { border-bottom: 1px solid #e8e8e8; }
                .accepted, .expand { background-color: #92D050; }
                .rfa { background-color: #C6EFCE; }
                .devReview { background-color: #A3EDFF; }
                .dev { background-color: #CCD4DE; }
                .qa { background-color: #66FFCC; }
                table { text-align: left; border-collapse: collapse; width: 100%; }
                th, td { padding: 4px 8px; }
                .charts-table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                .charts-table td { border: none; text-align: center; vertical-align: top; padding: 10px; }
                .chart-image { max-width: 280px; height: auto; border: 1px solid #ddd; }
                .chart-title { font-weight: bold; margin-bottom: 8px; font-size: 14px; }
                </style>";ates email logic from controller and uses configuration for SMTP settings
 */
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace JobCompare.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _env;

        public EmailService(IOptions<EmailSettings> emailSettings, IWebHostEnvironment env)
        {
            _emailSettings = emailSettings.Value;
            _env = env;
        }

        public async Task<bool> JobCompareEmailAsync(EmailRequest request)
        {
            try
            {
                // Use ToAddress from settings, no project key logic
                var toAddress = _emailSettings.DefaultBccAddress;
                var html = BuildEmailHtml(request);
                var subject = request.Subject ?? "Amazon Order Request";

                using var smtp = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort);
                smtp.Credentials = new System.Net.NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                // Explicitly enable TLS (STARTTLS) for Zoho SMTP on port 587
                smtp.EnableSsl = true;

                using var mail = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromAddress, _emailSettings.FromDisplayName),
                    Subject = subject,
                    Body = html,
                    IsBodyHtml = true
                };

                // Add recipients
                mail.To.Add(toAddress);
                if (!string.IsNullOrWhiteSpace(_emailSettings.DefaultBccAddress))
                    mail.Bcc.Add(_emailSettings.DefaultBccAddress);

                await smtp.SendMailAsync(mail);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string BuildEmailHtml(EmailRequest request)
        {
            var css = @"
                <style>
                body, table, th, td { font-family: Calibri, Arial, sans-serif; }
                thead, th { background-color: #73B6AD; }
                td { border-bottom: 1px solid #e8e8e8; }
                table { text-align: left; border-collapse: collapse; width: 100%; }
                th, td { padding: 4px 8px; }
                </style>
            ";

            // Build a table of products from request.Html (which is the order summary)
            var orderSummary = request.Html ?? "";

            return $@"
                <html>
                <head>{css}</head>
                <body>
                    <h2>Amazon Order Request</h2>
                    {orderSummary}
                </body>
                </html>
            ";
        }

        private string GetChartDisplayName(string chartId)
        {
            return chartId.ToLowerInvariant() switch
            {
                var id when id.Contains("pie") => "Status Distribution",
                var id when id.Contains("burndown") => "Burndown Chart",
                var id when id.Contains("workload") || id.Contains("bar") => "Story Workload",
                _ => "Chart"
            };
        }

        // No logo for Amazon order emails
        private void AttachLogo(MailMessage mail) { }

        // No project-to-email mapping needed for Amazon order emails
    }

    // Configuration model for email settings
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string FromAddress { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = string.Empty;
        public string DefaultBccAddress { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

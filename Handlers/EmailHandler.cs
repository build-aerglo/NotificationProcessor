using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using NotificationProcessor.Models;

namespace NotificationProcessor.Handlers;

public class EmailHandler : IEmailHandler
{
    private readonly ILogger<EmailHandler> _logger;

    public EmailHandler(ILogger<EmailHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(EmailNotificationData emailData)
    {
        try
        {
            // Get SMTP configuration from environment
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            var fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? smtpUser;

            // Validate required fields
            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogError("Missing SMTP configuration in environment variables");
                return false;
            }

            if (string.IsNullOrEmpty(emailData.To))
            {
                _logger.LogError("Missing 'To' field in email notification data");
                return false;
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Notification System", fromEmail));
            message.To.Add(new MailboxAddress("", emailData.To));
            message.Subject = emailData.Subject;

            // Build message body
            var builder = new BodyBuilder
            {
                TextBody = emailData.Body
            };

            if (!string.IsNullOrEmpty(emailData.Html))
            {
                builder.HtmlBody = emailData.Html;
            }

            message.Body = builder.ToMessageBody();

            // Send email
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To}", emailData.To);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            return false;
        }
    }
}

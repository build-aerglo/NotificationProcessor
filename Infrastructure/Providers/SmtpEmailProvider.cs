using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using NotificationProcessor.Domain.Entities;
using NotificationProcessor.Domain.Interfaces;

namespace NotificationProcessor.Infrastructure.Providers;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(ILogger<SmtpEmailProvider> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(EmailNotification emailNotification)
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

            if (!emailNotification.IsValid())
            {
                _logger.LogError("Invalid email notification data");
                return false;
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Notification System", fromEmail));
            message.To.Add(new MailboxAddress("", emailNotification.To));
            message.Subject = emailNotification.Subject;

            // Build message body
            var builder = new BodyBuilder
            {
                TextBody = emailNotification.Body
            };

            if (!string.IsNullOrEmpty(emailNotification.Html))
            {
                builder.HtmlBody = emailNotification.Html;
            }

            message.Body = builder.ToMessageBody();

            // Send email
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To}", emailNotification.To);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            return false;
        }
    }
}

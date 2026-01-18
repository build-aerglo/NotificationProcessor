using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly SmtpConfiguration _smtpConfig;

    public EmailSender(ILogger<EmailSender> logger, SmtpConfiguration smtpConfig)
    {
        _logger = logger;
        _smtpConfig = smtpConfig ?? throw new ArgumentNullException(nameof(smtpConfig));

        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_smtpConfig.Host))
            throw new ArgumentException("SMTP Host cannot be empty");

        if (string.IsNullOrEmpty(_smtpConfig.Username))
            throw new ArgumentException("SMTP Username cannot be empty");

        if (string.IsNullOrEmpty(_smtpConfig.Password))
            throw new ArgumentException("SMTP Password cannot be empty");

        if (string.IsNullOrEmpty(_smtpConfig.FromEmail))
            throw new ArgumentException("SMTP FromEmail cannot be empty");
    }

    public async Task<bool> SendEmailAsync(string recipient, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(recipient))
        {
            _logger.LogError("Recipient email address cannot be empty");
            return false;
        }

        if (string.IsNullOrEmpty(subject))
        {
            _logger.LogWarning("Email subject is empty");
        }

        try
        {
            using var smtpClient = new SmtpClient(_smtpConfig.Host, _smtpConfig.Port)
            {
                EnableSsl = _smtpConfig.EnableSsl,
                Credentials = new NetworkCredential(_smtpConfig.Username, _smtpConfig.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30 seconds timeout
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpConfig.FromEmail, _smtpConfig.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(recipient);

            _logger.LogInformation("Sending email to {Recipient} with subject: {Subject}", recipient, subject);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Recipient}: {ErrorMessage}", recipient, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", recipient);
            return false;
        }
    }
}

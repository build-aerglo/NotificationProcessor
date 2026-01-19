using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly AzureEmailConfiguration _emailConfig;
    private readonly EmailClient _emailClient;

    public EmailSender(ILogger<EmailSender> logger, AzureEmailConfiguration emailConfig)
    {
        _logger = logger;
        _emailConfig = emailConfig ?? throw new ArgumentNullException(nameof(emailConfig));

        ValidateConfiguration();
        _emailClient = new EmailClient(_emailConfig.ConnectionString);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_emailConfig.ConnectionString))
            throw new ArgumentException("Azure Communication Services Connection String cannot be empty");

        if (string.IsNullOrEmpty(_emailConfig.SenderAddress))
            throw new ArgumentException("Sender Address cannot be empty");
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
            var emailMessage = new EmailMessage(
                senderAddress: _emailConfig.SenderAddress,
                content: new EmailContent(subject)
                {
                    Html = htmlBody
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                    new EmailAddress(recipient)
                }));

            _logger.LogInformation("Sending email to {Recipient} with subject: {Subject}", recipient, subject);

            EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
                WaitUntil.Completed,
                emailMessage);

            _logger.LogInformation("Email sent successfully to {Recipient}. Operation ID: {OperationId}",
                recipient, emailSendOperation.Id);

            return true;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services error sending email to {Recipient}: {ErrorMessage}",
                recipient, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", recipient);
            return false;
        }
    }
}

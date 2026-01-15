using NotificationProcessor.Models;

namespace NotificationProcessor.Handlers;

public interface IEmailHandler
{
    Task<bool> SendEmailAsync(EmailNotificationData emailData);
}

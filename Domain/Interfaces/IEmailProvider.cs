using NotificationProcessor.Domain.Entities;

namespace NotificationProcessor.Domain.Interfaces;

public interface IEmailProvider
{
    Task<bool> SendEmailAsync(EmailNotification emailNotification);
}

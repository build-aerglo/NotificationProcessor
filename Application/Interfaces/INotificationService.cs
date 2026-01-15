using NotificationProcessor.Domain.Entities;

namespace NotificationProcessor.Application.Interfaces;

public interface INotificationService
{
    Task<bool> ProcessNotificationAsync(NotificationMessage notification);
    Task<bool> SendEmailNotificationAsync(EmailNotification emailNotification);
    Task<bool> SendSmsNotificationAsync(SmsNotification smsNotification);
}

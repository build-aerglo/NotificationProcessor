using NotificationProcessor.Models;

namespace NotificationProcessor.Handlers;

public interface ISmsHandler
{
    Task<bool> SendSmsAsync(SmsNotificationData smsData);
}

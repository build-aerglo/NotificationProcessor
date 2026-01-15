using NotificationProcessor.Domain.Entities;

namespace NotificationProcessor.Domain.Interfaces;

public interface ISmsProvider
{
    Task<bool> SendSmsAsync(SmsNotification smsNotification);
}

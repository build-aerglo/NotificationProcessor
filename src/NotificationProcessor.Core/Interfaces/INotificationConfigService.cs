using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Core.Interfaces;

/// <summary>
/// Service interface for managing notification configurations
/// </summary>
public interface INotificationConfigService
{
    /// <summary>
    /// Get SMTP configuration for email notifications
    /// </summary>
    SmtpConfiguration GetSmtpConfiguration();

    /// <summary>
    /// Get Twilio configuration for SMS notifications
    /// </summary>
    TwilioConfiguration GetTwilioConfiguration();

    /// <summary>
    /// Get configuration by notification type
    /// </summary>
    NotificationConfigResponse GetConfiguration(string notificationType);
}

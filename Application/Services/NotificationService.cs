using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Application.Interfaces;
using NotificationProcessor.Domain.Entities;
using NotificationProcessor.Domain.Interfaces;

namespace NotificationProcessor.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailProvider _emailProvider;
    private readonly ISmsProvider _smsProvider;

    public NotificationService(
        ILogger<NotificationService> logger,
        IEmailProvider emailProvider,
        ISmsProvider smsProvider)
    {
        _logger = logger;
        _emailProvider = emailProvider;
        _smsProvider = smsProvider;
    }

    public async Task<bool> ProcessNotificationAsync(NotificationMessage notification)
    {
        if (!notification.IsValid())
        {
            _logger.LogError("Invalid notification message");
            return false;
        }

        _logger.LogInformation("Processing notification of type: {Type}", notification.Type);

        return notification.Type.ToLower() switch
        {
            "email" => await ProcessEmailNotificationAsync(notification.Data),
            "sms" => await ProcessSmsNotificationAsync(notification.Data),
            _ => throw new ArgumentException($"Unknown notification type: {notification.Type}")
        };
    }

    public async Task<bool> SendEmailNotificationAsync(EmailNotification emailNotification)
    {
        if (!emailNotification.IsValid())
        {
            _logger.LogError("Invalid email notification data");
            return false;
        }

        _logger.LogInformation("Sending email notification to {To}", emailNotification.To);
        return await _emailProvider.SendEmailAsync(emailNotification);
    }

    public async Task<bool> SendSmsNotificationAsync(SmsNotification smsNotification)
    {
        if (!smsNotification.IsValid())
        {
            _logger.LogError("Invalid SMS notification data");
            return false;
        }

        _logger.LogInformation("Sending SMS notification to {To}", smsNotification.To);
        return await _smsProvider.SendSmsAsync(smsNotification);
    }

    private async Task<bool> ProcessEmailNotificationAsync(object? data)
    {
        try
        {
            var emailNotification = JsonSerializer.Deserialize<EmailNotification>(
                JsonSerializer.Serialize(data),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (emailNotification == null)
            {
                _logger.LogError("Failed to deserialize email notification data");
                return false;
            }

            return await SendEmailNotificationAsync(emailNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email notification");
            return false;
        }
    }

    private async Task<bool> ProcessSmsNotificationAsync(object? data)
    {
        try
        {
            var smsNotification = JsonSerializer.Deserialize<SmsNotification>(
                JsonSerializer.Serialize(data),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (smsNotification == null)
            {
                _logger.LogError("Failed to deserialize SMS notification data");
                return false;
            }

            return await SendSmsNotificationAsync(smsNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SMS notification");
            return false;
        }
    }
}

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Handlers;
using NotificationProcessor.Models;

namespace NotificationProcessor.Functions;

public class NotificationQueueTrigger
{
    private readonly ILogger<NotificationQueueTrigger> _logger;
    private readonly IEmailHandler _emailHandler;
    private readonly ISmsHandler _smsHandler;

    public NotificationQueueTrigger(
        ILogger<NotificationQueueTrigger> logger,
        IEmailHandler emailHandler,
        ISmsHandler smsHandler)
    {
        _logger = logger;
        _emailHandler = emailHandler;
        _smsHandler = smsHandler;
    }

    [Function("NotificationQueueTrigger")]
    public async Task Run(
        [QueueTrigger("notifications", Connection = "AZURE_STORAGE_CONNECTION_STRING")] string queueMessage)
    {
        _logger.LogInformation("Processing queue message: {Message}", queueMessage);

        try
        {
            // Parse the notification message
            var notification = JsonSerializer.Deserialize<NotificationMessage>(queueMessage, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (notification == null)
            {
                _logger.LogError("Failed to deserialize notification message");
                return;
            }

            if (string.IsNullOrEmpty(notification.Type))
            {
                _logger.LogError("Missing 'Type' field in notification message");
                return;
            }

            if (notification.Data == null)
            {
                _logger.LogError("Missing 'Data' field in notification message");
                return;
            }

            // Process based on notification type
            switch (notification.Type.ToLower())
            {
                case "email":
                    await ProcessEmailNotification(notification.Data);
                    break;

                case "sms":
                    await ProcessSmsNotification(notification.Data);
                    break;

                default:
                    _logger.LogError("Unknown notification type: {Type}", notification.Type);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in queue message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queue message");
            throw; // Re-throw to allow Azure Functions to handle retry logic
        }
    }

    private async Task ProcessEmailNotification(object data)
    {
        try
        {
            _logger.LogInformation("Processing email notification");

            var emailData = JsonSerializer.Deserialize<EmailNotificationData>(
                JsonSerializer.Serialize(data),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (emailData == null)
            {
                _logger.LogError("Failed to deserialize email notification data");
                return;
            }

            var success = await _emailHandler.SendEmailAsync(emailData);

            if (success)
            {
                _logger.LogInformation("Email notification processed successfully");
            }
            else
            {
                _logger.LogError("Failed to process email notification");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email notification");
        }
    }

    private async Task ProcessSmsNotification(object data)
    {
        try
        {
            _logger.LogInformation("Processing SMS notification");

            var smsData = JsonSerializer.Deserialize<SmsNotificationData>(
                JsonSerializer.Serialize(data),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (smsData == null)
            {
                _logger.LogError("Failed to deserialize SMS notification data");
                return;
            }

            var success = await _smsHandler.SendSmsAsync(smsData);

            if (success)
            {
                _logger.LogInformation("SMS notification processed successfully");
            }
            else
            {
                _logger.LogError("Failed to process SMS notification");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SMS notification");
        }
    }
}

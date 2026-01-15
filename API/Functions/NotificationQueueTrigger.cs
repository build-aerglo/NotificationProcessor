using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Application.Interfaces;
using NotificationProcessor.Domain.Entities;

namespace NotificationProcessor.API.Functions;

public class NotificationQueueTrigger
{
    private readonly ILogger<NotificationQueueTrigger> _logger;
    private readonly INotificationService _notificationService;

    public NotificationQueueTrigger(
        ILogger<NotificationQueueTrigger> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
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

            // Process notification through application service
            var success = await _notificationService.ProcessNotificationAsync(notification);

            if (success)
            {
                _logger.LogInformation("Notification processed successfully");
            }
            else
            {
                _logger.LogError("Failed to process notification");
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
}

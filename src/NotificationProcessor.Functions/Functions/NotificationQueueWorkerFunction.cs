using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Functions.Functions;

public class NotificationQueueWorkerFunction
{
    private readonly ILogger<NotificationQueueWorkerFunction> _logger;
    private readonly INotificationProcessor _notificationProcessor;

    public NotificationQueueWorkerFunction(
        ILogger<NotificationQueueWorkerFunction> logger,
        INotificationProcessor notificationProcessor)
    {
        _logger = logger;
        _notificationProcessor = notificationProcessor;
    }

    [Function("NotificationQueueWorker")]
    public async Task Run(
        [QueueTrigger("notifications", Connection = "AzureQueueStorage:ConnectionString")]
        QueueMessage message)
    {
        _logger.LogInformation("Queue trigger function processing message: {MessageId}", message.MessageId);

        try
        {
            // Deserialize the message
            var notification = JsonSerializer.Deserialize<NotificationMessage>(message.MessageText);

            if (notification == null)
            {
                _logger.LogError(
                    "Failed to deserialize notification message. MessageId: {MessageId}, DequeueCount: {DequeueCount}, MessageText: {MessageText}",
                    message.MessageId, message.DequeueCount, message.MessageText);
                return;
            }

            _logger.LogInformation(
                "Processing queue message. MessageId: {MessageId}, NotificationId: {NotificationId}, DequeueCount: {DequeueCount}, Template: {Template}, Channel: {Channel}",
                message.MessageId, notification.Id, message.DequeueCount, notification.Template, notification.Channel);

            // Process the notification
            var success = await _notificationProcessor.ProcessNotificationAsync(notification);

            if (success)
            {
                _logger.LogInformation(
                    "Successfully processed notification {NotificationId} from queue (MessageId: {MessageId})",
                    notification.Id, message.MessageId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to process notification {NotificationId}. MessageId: {MessageId}, DequeueCount: {DequeueCount}, Template: {Template}, Channel: {Channel}. Message will be retried based on queue configuration.",
                    notification.Id, message.MessageId, message.DequeueCount, notification.Template, notification.Channel);

                // Azure Queue Storage will automatically retry based on visibility timeout
                // If max dequeue count is reached, message will go to poison queue
                throw new Exception($"Failed to process notification {notification.Id} (MessageId: {message.MessageId}, DequeueCount: {message.DequeueCount})");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Invalid JSON format in queue message. MessageId: {MessageId}, DequeueCount: {DequeueCount}, MessageText: {MessageText}",
                message.MessageId, message.DequeueCount, message.MessageText);
            // Don't throw - this will remove the invalid message from queue
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing queue message. MessageId: {MessageId}, DequeueCount: {DequeueCount}, ExceptionType: {ExceptionType}, Message: {ErrorMessage}",
                message.MessageId, message.DequeueCount, ex.GetType().Name, ex.Message);
            // Re-throw to trigger Azure Queue retry mechanism
            throw;
        }
    }
}

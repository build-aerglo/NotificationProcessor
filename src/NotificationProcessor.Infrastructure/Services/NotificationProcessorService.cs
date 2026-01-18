using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Infrastructure.Services;

public class NotificationProcessorService : INotificationProcessor
{
    private readonly ILogger<NotificationProcessorService> _logger;
    private readonly ITemplateEngine _templateEngine;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly INotificationRepository _notificationRepository;
    private const int MaxRetries = 5;

    public NotificationProcessorService(
        ILogger<NotificationProcessorService> logger,
        ITemplateEngine templateEngine,
        IEmailSender emailSender,
        ISmsSender smsSender,
        INotificationRepository notificationRepository)
    {
        _logger = logger;
        _templateEngine = templateEngine ?? throw new ArgumentNullException(nameof(templateEngine));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
    }

    public async Task<bool> ProcessNotificationAsync(NotificationMessage notification)
    {
        if (notification == null)
        {
            _logger.LogError("Notification message cannot be null");
            return false;
        }

        var notificationContext = GetNotificationContext(notification);

        _logger.LogInformation(
            "Processing notification {NotificationId} - Template: {Template}, Channel: {Channel}, Retry: {RetryCount}, Context: {Context}",
            notification.Id, notification.Template, notification.Channel, notification.RetryCount, notificationContext);

        try
        {
            // Check if max retries exceeded
            if (notification.RetryCount >= MaxRetries)
            {
                _logger.LogError(
                    "Notification {NotificationId} exceeded max retries ({MaxRetries}). Moving to failed status.",
                    notification.Id, MaxRetries);

                await _notificationRepository.MarkAsFailedAsync(notification.Id, notification.RetryCount);
                return false;
            }

            // Load and render template
            string template;
            try
            {
                template = await _templateEngine.LoadTemplateAsync(notification.Template, notification.Channel);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex,
                    "Template {Template} not found for channel {Channel}. Notification {NotificationId} will be marked as failed. Context: {Context}",
                    notification.Template, notification.Channel, notification.Id, notificationContext);

                await _notificationRepository.MarkAsFailedAsync(notification.Id, notification.RetryCount);
                return false;
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex,
                    "Channel directory {Channel} not found. Notification {NotificationId} will be marked as failed. Context: {Context}",
                    notification.Channel, notification.Id, notificationContext);

                await _notificationRepository.MarkAsFailedAsync(notification.Id, notification.RetryCount);
                return false;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex,
                    "Invalid argument for notification {NotificationId}. Template: {Template}, Channel: {Channel}. Context: {Context}",
                    notification.Id, notification.Template, notification.Channel, notificationContext);

                await _notificationRepository.MarkAsFailedAsync(notification.Id, notification.RetryCount);
                return false;
            }

            var renderedContent = _templateEngine.Render(template, notification.Payload);

            // Send notification based on channel
            bool sent = false;
            switch (notification.Channel.ToLowerInvariant())
            {
                case NotificationChannel.Email:
                    sent = await SendEmailNotificationAsync(notification, renderedContent);
                    break;

                case NotificationChannel.Sms:
                    sent = await SendSmsNotificationAsync(notification, renderedContent);
                    break;

                case NotificationChannel.InApp:
                    _logger.LogWarning(
                        "In-app notifications not yet implemented for notification {NotificationId}",
                        notification.Id);
                    sent = false;
                    break;

                default:
                    _logger.LogError(
                        "Unknown channel {Channel} for notification {NotificationId}. Context: {Context}",
                        notification.Channel, notification.Id, notificationContext);
                    await _notificationRepository.MarkAsFailedAsync(notification.Id, notification.RetryCount);
                    return false;
            }

            if (sent)
            {
                // Mark as delivered
                await _notificationRepository.MarkAsDeliveredAsync(notification.Id, DateTime.UtcNow);
                _logger.LogInformation("Notification {NotificationId} processed successfully", notification.Id);
                return true;
            }
            else
            {
                // Increment retry count and potentially requeue
                var newRetryCount = notification.RetryCount + 1;
                await _notificationRepository.UpdateRetryCountAsync(notification.Id, newRetryCount);

                _logger.LogWarning(
                    "Notification {NotificationId} failed to send. Retry count: {RetryCount}",
                    notification.Id, newRetryCount);

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing notification {NotificationId}. " +
                "Template: {Template}, Channel: {Channel}, RetryCount: {RetryCount}, " +
                "Exception Type: {ExceptionType}, Context: {Context}",
                notification.Id, notification.Template, notification.Channel, notification.RetryCount,
                ex.GetType().Name, notificationContext);

            // Increment retry count
            var newRetryCount = notification.RetryCount + 1;
            await _notificationRepository.UpdateRetryCountAsync(notification.Id, newRetryCount);

            return false;
        }
    }

    private async Task<bool> SendEmailNotificationAsync(NotificationMessage notification, string htmlContent)
    {
        // Extract subject from payload or use default
        var subject = notification.Payload.ContainsKey("subject")
            ? notification.Payload["subject"]?.ToString() ?? "Notification"
            : "Notification";

        return await _emailSender.SendEmailAsync(notification.Recipient, subject, htmlContent);
    }

    private async Task<bool> SendSmsNotificationAsync(NotificationMessage notification, string messageContent)
    {
        return await _smsSender.SendSmsAsync(notification.Recipient, messageContent);
    }

    private string GetNotificationContext(NotificationMessage notification)
    {
        try
        {
            return JsonSerializer.Serialize(new
            {
                notification.Id,
                notification.Template,
                notification.Channel,
                notification.Recipient,
                notification.RetryCount,
                notification.RequestedAt,
                PayloadKeys = notification.Payload?.Keys.ToList() ?? new List<string>(),
                Payload = notification.Payload
            }, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize notification context for {NotificationId}", notification.Id);
            return $"{{\"Id\":\"{notification.Id}\",\"SerializationError\":\"{ex.Message}\"}}";
        }
    }
}

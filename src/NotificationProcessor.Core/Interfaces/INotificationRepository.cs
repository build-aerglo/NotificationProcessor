namespace NotificationProcessor.Core.Interfaces;

public interface INotificationRepository
{
    /// <summary>
    /// Updates a notification status to delivered
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="deliveredAt">Delivery timestamp</param>
    Task MarkAsDeliveredAsync(string notificationId, DateTime deliveredAt);

    /// <summary>
    /// Updates a notification status to failed
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="retryCount">Current retry count</param>
    Task MarkAsFailedAsync(string notificationId, int retryCount);

    /// <summary>
    /// Increments the retry count for a notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="retryCount">New retry count</param>
    Task UpdateRetryCountAsync(string notificationId, int retryCount);
}

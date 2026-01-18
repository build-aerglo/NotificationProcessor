using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Core.Interfaces;

public interface INotificationProcessor
{
    /// <summary>
    /// Processes a notification message
    /// </summary>
    /// <param name="notification">Notification message to process</param>
    /// <returns>True if processed successfully, false otherwise</returns>
    Task<bool> ProcessNotificationAsync(NotificationMessage notification);
}

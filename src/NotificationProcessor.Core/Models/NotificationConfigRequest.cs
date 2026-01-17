namespace NotificationProcessor.Core.Models;

/// <summary>
/// Request model for notification configuration
/// </summary>
public class NotificationConfigRequest
{
    public string NotificationType { get; set; } = string.Empty; // "email" or "sms"
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

namespace NotificationProcessor.Core.Models;

/// <summary>
/// Response model for notification configuration
/// </summary>
public class NotificationConfigResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public object? Configuration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

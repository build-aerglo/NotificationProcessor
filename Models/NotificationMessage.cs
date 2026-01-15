namespace NotificationProcessor.Models;

public class NotificationMessage
{
    public string Type { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class EmailNotificationData
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = "Notification";
    public string Body { get; set; } = string.Empty;
    public string? Html { get; set; }
}

public class SmsNotificationData
{
    public string To { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

namespace NotificationProcessor.Domain.Entities;

public class SmsNotification
{
    public string To { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(To) &&
               !string.IsNullOrEmpty(Body) &&
               Body.Length <= 1600; // SMS max length
    }
}

namespace NotificationProcessor.Domain.Entities;

public class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public object? Data { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Type) && Data != null;
    }
}

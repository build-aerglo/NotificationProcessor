namespace NotificationProcessor.Domain.Entities;

public class EmailNotification
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = "Notification";
    public string Body { get; set; } = string.Empty;
    public string? Html { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(To) &&
               !string.IsNullOrEmpty(Body) &&
               IsValidEmail(To);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

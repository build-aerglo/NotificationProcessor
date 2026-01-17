namespace NotificationProcessor.Core.Models;

/// <summary>
/// Twilio configuration for SMS notifications
/// </summary>
public class TwilioConfiguration
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
}

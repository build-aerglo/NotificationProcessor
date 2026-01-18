namespace NotificationProcessor.Core.Interfaces;

public interface ISmsSender
{
    /// <summary>
    /// Sends an SMS message
    /// </summary>
    /// <param name="recipient">Recipient phone number</param>
    /// <param name="message">SMS message content</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendSmsAsync(string recipient, string message);
}

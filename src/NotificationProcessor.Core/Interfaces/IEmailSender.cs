namespace NotificationProcessor.Core.Interfaces;

public interface IEmailSender
{
    /// <summary>
    /// Sends an email message
    /// </summary>
    /// <param name="recipient">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body content</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(string recipient, string subject, string htmlBody);
}

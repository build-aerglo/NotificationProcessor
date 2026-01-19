namespace NotificationProcessor.Core.Models;

/// <summary>
/// Azure Communication Services Email configuration
/// </summary>
public class AzureEmailConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
}

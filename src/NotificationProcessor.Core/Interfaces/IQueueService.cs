namespace NotificationProcessor.Core.Interfaces;

/// <summary>
/// Service interface for Azure Queue Storage operations
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Send message to Azure Queue
    /// </summary>
    Task<bool> SendMessageAsync(string message);

    /// <summary>
    /// Send configuration to queue
    /// </summary>
    Task<bool> SendConfigurationAsync<T>(T configuration) where T : class;
}

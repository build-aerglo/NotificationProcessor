using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using System.Text.Json;

namespace NotificationProcessor.Infrastructure.Services;

/// <summary>
/// Azure Queue Storage service implementation
/// </summary>
public class QueueService : IQueueService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<QueueService> _logger;

    public QueueService(IConfiguration configuration, ILogger<QueueService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureQueueStorage:ConnectionString"];
        var queueName = configuration["AzureQueueStorage:QueueName"] ?? "notifications";

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Queue Storage connection string is not configured");
        }

        _queueClient = new QueueClient(connectionString, queueName);
        _queueClient.CreateIfNotExists();
    }

    public async Task<bool> SendMessageAsync(string message)
    {
        try
        {
            var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
            await _queueClient.SendMessageAsync(base64Message);
            _logger.LogInformation("Message sent to queue successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to queue");
            return false;
        }
    }

    public async Task<bool> SendConfigurationAsync<T>(T configuration) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            return await SendMessageAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing and sending configuration to queue");
            return false;
        }
    }
}

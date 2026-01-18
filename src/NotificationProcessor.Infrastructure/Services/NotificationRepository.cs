using Microsoft.Extensions.Logging;
using Npgsql;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Infrastructure.Services;

public class NotificationRepository : INotificationRepository
{
    private readonly ILogger<NotificationRepository> _logger;
    private readonly string _connectionString;

    public NotificationRepository(ILogger<NotificationRepository> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task MarkAsDeliveredAsync(string notificationId, DateTime deliveredAt)
    {
        if (string.IsNullOrEmpty(notificationId))
        {
            throw new ArgumentException("Notification ID cannot be empty", nameof(notificationId));
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                UPDATE notifications
                SET status = @status, delivered_at = @deliveredAt
                WHERE id = @id";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", Guid.Parse(notificationId));
            command.Parameters.AddWithValue("@status", NotificationStatus.Delivered);
            command.Parameters.AddWithValue("@deliveredAt", deliveredAt);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                _logger.LogWarning("No notification found with ID: {NotificationId}", notificationId);
            }
            else
            {
                _logger.LogInformation("Marked notification {NotificationId} as delivered", notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as delivered", notificationId);
            throw;
        }
    }

    public async Task MarkAsFailedAsync(string notificationId, int retryCount)
    {
        if (string.IsNullOrEmpty(notificationId))
        {
            throw new ArgumentException("Notification ID cannot be empty", nameof(notificationId));
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                UPDATE notifications
                SET status = @status, retry_count = @retryCount
                WHERE id = @id";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", Guid.Parse(notificationId));
            command.Parameters.AddWithValue("@status", NotificationStatus.Failed);
            command.Parameters.AddWithValue("@retryCount", retryCount);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                _logger.LogWarning("No notification found with ID: {NotificationId}", notificationId);
            }
            else
            {
                _logger.LogInformation("Marked notification {NotificationId} as failed with retry count {RetryCount}",
                    notificationId, retryCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as failed", notificationId);
            throw;
        }
    }

    public async Task UpdateRetryCountAsync(string notificationId, int retryCount)
    {
        if (string.IsNullOrEmpty(notificationId))
        {
            throw new ArgumentException("Notification ID cannot be empty", nameof(notificationId));
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                UPDATE notifications
                SET retry_count = @retryCount
                WHERE id = @id";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", Guid.Parse(notificationId));
            command.Parameters.AddWithValue("@retryCount", retryCount);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                _logger.LogWarning("No notification found with ID: {NotificationId}", notificationId);
            }
            else
            {
                _logger.LogInformation("Updated retry count for notification {NotificationId} to {RetryCount}",
                    notificationId, retryCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating retry count for notification {NotificationId}", notificationId);
            throw;
        }
    }
}

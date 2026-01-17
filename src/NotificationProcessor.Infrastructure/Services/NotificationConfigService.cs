using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Infrastructure.Services;

/// <summary>
/// Service for managing notification configurations
/// </summary>
public class NotificationConfigService : INotificationConfigService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationConfigService> _logger;

    public NotificationConfigService(IConfiguration configuration, ILogger<NotificationConfigService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public SmtpConfiguration GetSmtpConfiguration()
    {
        try
        {
            var config = new SmtpConfiguration();
            _configuration.GetSection("Smtp").Bind(config);

            _logger.LogInformation("SMTP configuration retrieved successfully");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SMTP configuration");
            throw;
        }
    }

    public TwilioConfiguration GetTwilioConfiguration()
    {
        try
        {
            var config = new TwilioConfiguration();
            _configuration.GetSection("Twilio").Bind(config);

            _logger.LogInformation("Twilio configuration retrieved successfully");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Twilio configuration");
            throw;
        }
    }

    public NotificationConfigResponse GetConfiguration(string notificationType)
    {
        try
        {
            var response = new NotificationConfigResponse
            {
                NotificationType = notificationType,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            switch (notificationType.ToLowerInvariant())
            {
                case "email":
                case "smtp":
                    response.Configuration = GetSmtpConfiguration();
                    response.Success = true;
                    break;

                case "sms":
                case "twilio":
                    response.Configuration = GetTwilioConfiguration();
                    response.Success = true;
                    break;

                default:
                    response.Success = false;
                    response.ErrorMessage = $"Unknown notification type: {notificationType}";
                    _logger.LogWarning("Unknown notification type requested: {NotificationType}", notificationType);
                    break;
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration for type: {NotificationType}", notificationType);
            return new NotificationConfigResponse
            {
                NotificationType = notificationType,
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}

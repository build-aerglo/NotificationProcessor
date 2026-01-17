using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;
using System.Net;
using System.Text.Json;

namespace NotificationProcessor.Functions.Functions;

/// <summary>
/// Azure Function to send notification configuration to queue
/// </summary>
public class SendConfigToQueueFunction
{
    private readonly ILogger<SendConfigToQueueFunction> _logger;
    private readonly INotificationConfigService _configService;
    private readonly IQueueService _queueService;

    public SendConfigToQueueFunction(
        ILogger<SendConfigToQueueFunction> logger,
        INotificationConfigService configService,
        IQueueService queueService)
    {
        _logger = logger;
        _configService = configService;
        _queueService = queueService;
    }

    [Function("SendConfigToQueue")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "config/queue")] HttpRequestData req)
    {
        _logger.LogInformation("SendConfigToQueue function triggered");

        try
        {
            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<NotificationConfigRequest>(requestBody);

            if (request == null || string.IsNullOrEmpty(request.NotificationType))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request. NotificationType is required." });
                return badResponse;
            }

            // Get configuration
            var configResponse = _configService.GetConfiguration(request.NotificationType);

            if (!configResponse.Success)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(configResponse);
                return errorResponse;
            }

            // Send to queue
            var queueSuccess = await _queueService.SendConfigurationAsync(configResponse);

            if (!queueSuccess)
            {
                var queueErrorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await queueErrorResponse.WriteAsJsonAsync(new
                {
                    error = "Failed to send configuration to queue",
                    configuration = configResponse
                });
                return queueErrorResponse;
            }

            // Success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Configuration sent to queue successfully",
                requestId = configResponse.RequestId,
                notificationType = request.NotificationType
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending configuration to queue");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "An error occurred processing the request" });
            return response;
        }
    }
}

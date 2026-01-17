using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using System.Net;
using System.Text.Json;

namespace NotificationProcessor.Functions.Functions;

/// <summary>
/// Azure Function to retrieve SMTP configuration
/// </summary>
public class GetSmtpConfigFunction
{
    private readonly ILogger<GetSmtpConfigFunction> _logger;
    private readonly INotificationConfigService _configService;

    public GetSmtpConfigFunction(
        ILogger<GetSmtpConfigFunction> logger,
        INotificationConfigService configService)
    {
        _logger = logger;
        _configService = configService;
    }

    [Function("GetSmtpConfig")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "config/smtp")] HttpRequestData req)
    {
        _logger.LogInformation("GetSmtpConfig function triggered");

        try
        {
            var config = _configService.GetSmtpConfiguration();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(config);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SMTP configuration");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "Failed to retrieve SMTP configuration" });
            return response;
        }
    }
}

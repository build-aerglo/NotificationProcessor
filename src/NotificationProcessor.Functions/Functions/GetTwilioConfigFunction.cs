using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using System.Net;

namespace NotificationProcessor.Functions.Functions;

/// <summary>
/// Azure Function to retrieve Twilio configuration
/// </summary>
public class GetTwilioConfigFunction
{
    private readonly ILogger<GetTwilioConfigFunction> _logger;
    private readonly INotificationConfigService _configService;

    public GetTwilioConfigFunction(
        ILogger<GetTwilioConfigFunction> logger,
        INotificationConfigService configService)
    {
        _logger = logger;
        _configService = configService;
    }

    [Function("GetTwilioConfig")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "config/twilio")] HttpRequestData req)
    {
        _logger.LogInformation("GetTwilioConfig function triggered");

        try
        {
            var config = _configService.GetTwilioConfiguration();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(config);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Twilio configuration");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "Failed to retrieve Twilio configuration" });
            return response;
        }
    }
}

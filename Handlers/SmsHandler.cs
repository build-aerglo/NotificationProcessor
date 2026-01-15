using Microsoft.Extensions.Logging;
using NotificationProcessor.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;

namespace NotificationProcessor.Handlers;

public class SmsHandler : ISmsHandler
{
    private readonly ILogger<SmsHandler> _logger;

    public SmsHandler(ILogger<SmsHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(SmsNotificationData smsData)
    {
        try
        {
            // Get Twilio configuration from environment
            var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
            var fromPhone = Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE");

            // Validate required fields
            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhone))
            {
                _logger.LogError("Missing Twilio configuration in environment variables");
                return false;
            }

            if (string.IsNullOrEmpty(smsData.To))
            {
                _logger.LogError("Missing 'To' field in SMS notification data");
                return false;
            }

            if (string.IsNullOrEmpty(smsData.Body))
            {
                _logger.LogError("Missing 'Body' field in SMS notification data");
                return false;
            }

            // Initialize Twilio client
            TwilioClient.Init(accountSid, authToken);

            // Send SMS
            var message = await MessageResource.CreateAsync(
                body: smsData.Body,
                from: new Twilio.Types.PhoneNumber(fromPhone),
                to: new Twilio.Types.PhoneNumber(smsData.To)
            );

            _logger.LogInformation("SMS sent successfully to {To}, SID: {Sid}", smsData.To, message.Sid);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Twilio API error: {Message} (Code: {Code})", ex.Message, ex.Code);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS");
            return false;
        }
    }
}

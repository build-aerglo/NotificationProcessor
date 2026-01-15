using Microsoft.Extensions.Logging;
using NotificationProcessor.Domain.Entities;
using NotificationProcessor.Domain.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;

namespace NotificationProcessor.Infrastructure.Providers;

public class TwilioSmsProvider : ISmsProvider
{
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(ILogger<TwilioSmsProvider> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(SmsNotification smsNotification)
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

            if (!smsNotification.IsValid())
            {
                _logger.LogError("Invalid SMS notification data");
                return false;
            }

            // Initialize Twilio client
            TwilioClient.Init(accountSid, authToken);

            // Send SMS
            var message = await MessageResource.CreateAsync(
                body: smsNotification.Body,
                from: new Twilio.Types.PhoneNumber(fromPhone),
                to: new Twilio.Types.PhoneNumber(smsNotification.To)
            );

            _logger.LogInformation("SMS sent successfully to {To}, SID: {Sid}", smsNotification.To, message.Sid);
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

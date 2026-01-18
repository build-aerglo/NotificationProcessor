using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NotificationProcessor.Infrastructure.Services;

public class SmsSender : ISmsSender
{
    private readonly ILogger<SmsSender> _logger;
    private readonly TwilioConfiguration _twilioConfig;

    public SmsSender(ILogger<SmsSender> logger, TwilioConfiguration twilioConfig)
    {
        _logger = logger;
        _twilioConfig = twilioConfig ?? throw new ArgumentNullException(nameof(twilioConfig));

        ValidateConfiguration();
        InitializeTwilio();
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_twilioConfig.AccountSid))
            throw new ArgumentException("Twilio AccountSid cannot be empty");

        if (string.IsNullOrEmpty(_twilioConfig.AuthToken))
            throw new ArgumentException("Twilio AuthToken cannot be empty");

        if (string.IsNullOrEmpty(_twilioConfig.FromPhoneNumber))
            throw new ArgumentException("Twilio FromPhoneNumber cannot be empty");
    }

    private void InitializeTwilio()
    {
        TwilioClient.Init(_twilioConfig.AccountSid, _twilioConfig.AuthToken);
    }

    public async Task<bool> SendSmsAsync(string recipient, string message)
    {
        if (string.IsNullOrEmpty(recipient))
        {
            _logger.LogError("Recipient phone number cannot be empty");
            return false;
        }

        if (string.IsNullOrEmpty(message))
        {
            _logger.LogError("SMS message cannot be empty");
            return false;
        }

        try
        {
            _logger.LogInformation("Sending SMS to {Recipient}", recipient);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_twilioConfig.FromPhoneNumber),
                to: new PhoneNumber(recipient)
            );

            if (messageResource.Status == MessageResource.StatusEnum.Failed ||
                messageResource.Status == MessageResource.StatusEnum.Undelivered)
            {
                _logger.LogError("SMS failed to send to {Recipient}. Status: {Status}, ErrorCode: {ErrorCode}",
                    recipient, messageResource.Status, messageResource.ErrorCode);
                return false;
            }

            _logger.LogInformation("SMS sent successfully to {Recipient}. SID: {MessageSid}, Status: {Status}",
                recipient, messageResource.Sid, messageResource.Status);

            return true;
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            _logger.LogError(ex, "Twilio API error sending SMS to {Recipient}: {ErrorMessage}",
                recipient, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending SMS to {Recipient}", recipient);
            return false;
        }
    }
}

# Integration Guide: NotificationProcessor with Notification API

## Overview

This document clarifies how **NotificationProcessor** (credentials service) integrates with your **Notification API** (message processing service).

## Architecture Separation

### NotificationProcessor (This Service)
**Purpose**: Secure credential storage and exposure

**Responsibilities**:
- Store SMTP credentials (host, port, username, password, from email)
- Store Twilio credentials (account SID, auth token, phone number)
- Expose credentials via HTTP endpoints
- Optionally send credentials to Azure Queue for async access

**Does NOT handle**:
- Template loading or processing
- Message formatting (HTML generation)
- Actual email/SMS sending
- Business logic for notifications

### Notification API (Your Existing Service)
**Purpose**: Process notification requests and send messages

**Responsibilities**:
- Receive notification requests with template data
- Load and process HTML email templates
- Format SMS text messages
- Merge template data with templates
- Call NotificationProcessor to get SMTP/Twilio credentials
- Send emails via SMTP using retrieved credentials
- Send SMS via Twilio using retrieved credentials
- Handle retries, logging, and delivery tracking

## Integration Patterns

### Pattern 1: Direct HTTP Integration (Recommended for Real-time)

```
User/System → Notification API → NotificationProcessor (get credentials)
                ↓
              SMTP/Twilio (send message)
```

**Flow**:
1. Notification API receives request:
   ```json
   {
     "notificationId": "uuid",
     "type": "UserWelcome",
     "channels": ["email", "sms"],
     "priority": "high",
     "recipient": {
       "userId": "123",
       "email": "user@example.com",
       "phone": "+123456789"
     },
     "payload": {
       "firstName": "John",
       "otp": "456789"
     },
     "requestedAt": "2026-01-14T12:00:00Z"
   }
   ```

2. Notification API determines which channels to use (email, sms)

3. **For Email Channel**:
   - Notification API calls: `GET https://your-function-app.azurewebsites.net/api/config/smtp`
   - Receives SMTP credentials:
     ```json
     {
       "host": "smtp.example.com",
       "port": 587,
       "username": "your-username",
       "password": "your-password",
       "fromEmail": "noreply@example.com",
       "fromName": "Notification System",
       "enableSsl": true
     }
     ```
   - Loads HTML template for "UserWelcome" type
   - Merges payload data (`firstName: "John"`) with template
   - Sends email using SMTP credentials

4. **For SMS Channel**:
   - Notification API calls: `GET https://your-function-app.azurewebsites.net/api/config/twilio`
   - Receives Twilio credentials:
     ```json
     {
       "accountSid": "AC1234567890",
       "authToken": "your-auth-token",
       "fromPhoneNumber": "+1234567890"
     }
     ```
   - Formats SMS text message with payload data
   - Sends SMS using Twilio SDK with retrieved credentials

### Pattern 2: Queue-based Integration (Recommended for High Volume)

```
User/System → Azure Queue (notification requests)
                ↓
           Notification API (consumer)
                ↓
           NotificationProcessor (get credentials)
                ↓
           SMTP/Twilio (send message)
```

**Flow**:
1. External system sends notification request to Azure Queue:
   ```json
   {
     "messageId": "uuid",
     "to": ["user@example.com"],
     "subject": "Welcome",
     "templateId": "welcome-email",
     "templateData": {
       "firstName": "John"
     },
     "priority": "high",
     "retryCount": 0,
     "requestedAt": "2026-01-14T12:00:00Z"
   }
   ```

2. Notification API consumes messages from queue

3. For each message:
   - Determines notification type (email/sms)
   - Calls NotificationProcessor to get credentials
   - Processes and sends notification
   - Removes message from queue on success

### Pattern 3: Hybrid - Credentials in Queue (Alternative)

If you want to avoid repeated HTTP calls for credentials:

```
Notification API → NotificationProcessor → Azure Queue (with credentials)
                                              ↓
                                         Notification API (consumer)
                                              ↓
                                         SMTP/Twilio (send message)
```

**Flow**:
1. Notification API calls: `POST /api/config/queue` with:
   ```json
   {
     "notificationType": "email"
   }
   ```

2. NotificationProcessor sends credentials to queue:
   ```json
   {
     "requestId": "guid",
     "notificationType": "email",
     "configuration": {
       "host": "smtp.example.com",
       "port": 587,
       "username": "your-username",
       "password": "your-password",
       ...
     },
     "success": true,
     "timestamp": "2026-01-17T12:00:00Z"
   }
   ```

3. Notification API caches these credentials in memory/Redis

4. Notification API uses cached credentials for sending messages

**Note**: This pattern is less secure (credentials in queue) but reduces HTTP calls.

## Recommended Architecture

Based on your sample requests, here's the recommended flow:

### For Template-based Email Notifications

```typescript
// In your Notification API

class NotificationService {
  async sendNotification(request: NotificationRequest) {
    // 1. Get SMTP credentials from NotificationProcessor
    const smtpConfig = await this.httpClient.get(
      'https://notification-processor.azurewebsites.net/api/config/smtp'
    );

    // 2. Load HTML template (from local files, database, or blob storage)
    const template = await this.templateEngine.load(request.type); // "UserWelcome"

    // 3. Merge template with payload data
    const htmlBody = template.render({
      firstName: request.payload.firstName,
      otp: request.payload.otp
    });

    // 4. Send email using SMTP credentials
    await this.emailSender.send({
      host: smtpConfig.host,
      port: smtpConfig.port,
      auth: {
        user: smtpConfig.username,
        pass: smtpConfig.password
      },
      from: `${smtpConfig.fromName} <${smtpConfig.fromEmail}>`,
      to: request.recipient.email,
      subject: this.getSubjectForType(request.type),
      html: htmlBody
    });
  }
}
```

### For SMS Notifications

```typescript
// In your Notification API

class SmsService {
  async sendSms(request: NotificationRequest) {
    // 1. Get Twilio credentials from NotificationProcessor
    const twilioConfig = await this.httpClient.get(
      'https://notification-processor.azurewebsites.net/api/config/twilio'
    );

    // 2. Format SMS text message
    const message = this.formatSmsMessage(request.type, request.payload);
    // e.g., "Welcome John! Your OTP is 456789"

    // 3. Send SMS using Twilio
    const twilio = require('twilio')(
      twilioConfig.accountSid,
      twilioConfig.authToken
    );

    await twilio.messages.create({
      from: twilioConfig.fromPhoneNumber,
      to: request.recipient.phone,
      body: message
    });
  }
}
```

## Data Flow Summary

### What Goes in Azure Queue (Your Choice)

**Option A - Notification Requests Only** (Recommended):
```json
{
  "notificationId": "uuid",
  "type": "UserWelcome",
  "channels": ["email", "sms"],
  "recipient": { ... },
  "payload": { ... }
}
```

**Option B - Formatted Messages Ready to Send**:
```json
{
  "messageId": "uuid",
  "to": ["user@example.com"],
  "subject": "Welcome",
  "htmlBody": "<html>...<h1>Welcome John</h1>...</html>",
  "textBody": "Welcome John!"
}
```

### What NotificationProcessor Provides

**SMTP Credentials** (via `GET /api/config/smtp`):
- Host, Port, Username, Password
- From Email, From Name
- SSL/TLS settings

**Twilio Credentials** (via `GET /api/config/twilio`):
- Account SID, Auth Token
- From Phone Number

## Security Considerations

1. **Credential Caching**: Cache SMTP/Twilio credentials in your Notification API for 5-10 minutes to reduce HTTP calls
2. **Function Keys**: Protect NotificationProcessor endpoints with Azure Function keys
3. **HTTPS Only**: Always use HTTPS for credential transmission
4. **Managed Identity**: Use Azure Managed Identity between services if possible
5. **Key Rotation**: When credentials change, invalidate cache in Notification API

## Answering Your Questions

> "Is this to be handled on the notification api?"

**YES** - The Notification API should handle:
- Receiving notification requests (with your JSON format)
- Template loading and HTML generation
- Message formatting for email and SMS
- Actual sending via SMTP/Twilio

> "Because the initial plan was it'll send html templates as the body or param for mails and a text for sms, so how will that integrate?"

**Integration**:
1. Your Notification API receives a request with `templateId: "welcome-email"` and `templateData: { firstName: "John" }`
2. Notification API loads the HTML template file (e.g., from `templates/welcome-email.html`)
3. Notification API merges template with data to produce final HTML: `<h1>Welcome John!</h1>...`
4. Notification API calls NotificationProcessor to get SMTP credentials
5. Notification API sends the final HTML body via SMTP using those credentials

**For SMS**:
1. Notification API receives SMS request with payload data
2. Notification API formats simple text message (no HTML)
3. Notification API calls NotificationProcessor to get Twilio credentials
4. Notification API sends text message via Twilio SDK

## Example Notification API Pseudocode

```csharp
public class NotificationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITemplateEngine _templateEngine;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;

    [HttpPost("api/notifications")]
    public async Task<IActionResult> SendNotification(NotificationRequest request)
    {
        foreach (var channel in request.Channels)
        {
            if (channel == "email")
            {
                // Get SMTP config from NotificationProcessor
                var smtpConfig = await GetSmtpConfig();

                // Load and render HTML template
                var htmlBody = await _templateEngine.RenderAsync(
                    request.Type,
                    request.Payload
                );

                // Send email
                await _emailSender.SendAsync(smtpConfig,
                    request.Recipient.Email,
                    GetSubject(request.Type),
                    htmlBody
                );
            }
            else if (channel == "sms")
            {
                // Get Twilio config from NotificationProcessor
                var twilioConfig = await GetTwilioConfig();

                // Format SMS text
                var smsText = FormatSmsMessage(request.Type, request.Payload);

                // Send SMS
                await _smsSender.SendAsync(twilioConfig,
                    request.Recipient.Phone,
                    smsText
                );
            }
        }

        return Ok(new { success = true });
    }

    private async Task<SmtpConfiguration> GetSmtpConfig()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            "https://notification-processor.azurewebsites.net/api/config/smtp"
        );
        return await response.Content.ReadFromJsonAsync<SmtpConfiguration>();
    }

    private async Task<TwilioConfiguration> GetTwilioConfig()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            "https://notification-processor.azurewebsites.net/api/config/twilio"
        );
        return await response.Content.ReadFromJsonAsync<TwilioConfiguration>();
    }
}
```

## Summary

- **NotificationProcessor**: Provides credentials (keys) only
- **Notification API**: Handles all notification logic, templates, and sending
- **Azure Queue**: Carries notification requests (not credentials, unless you choose Pattern 3)
- **Templates**: Stored and processed by Notification API
- **HTML/Text Body**: Generated by Notification API before sending

This separation keeps credentials secure and isolated while giving your Notification API full control over message formatting and delivery.

# Notification Queue Worker

This document describes the notification queue worker implementation for processing email and SMS notifications.

## Overview

The notification queue worker is a background Azure Function that:
1. Fetches notification messages from Azure Queue Storage
2. Loads and renders templates with dynamic data
3. Sends notifications via Email (Azure Communication Services) or SMS (Twilio)
4. Updates the database with delivery status
5. Implements retry logic with exponential backoff

## Architecture

### Components

1. **NotificationQueueWorkerFunction** - Azure Function with Queue Trigger
2. **NotificationProcessorService** - Orchestrates the notification processing
3. **TemplateEngine** - Loads and renders templates with `{{placeholder}}` syntax
4. **EmailSender** - Sends emails via Azure Communication Services
5. **SmsSender** - Sends SMS via Twilio
6. **NotificationRepository** - Updates notification status in PostgreSQL

### Queue Message Format

```json
{
  "id": "uuid",
  "template": "UserWelcome",
  "channel": "email | sms",
  "retryCount": 0,
  "recipient": "email@example.com | +1234567890",
  "payload": {
    "firstName": "John",
    "otp": "456789",
    "subject": "Welcome!"
  },
  "requestedAt": "2026-01-14T12:00:00Z"
}
```

### Template Structure

Templates are organized by channel in the `Templates` directory:

```
Templates/
├── email/
│   ├── UserWelcome.html
│   └── PasswordReset.html
└── sms/
    ├── UserWelcome.txt
    └── PasswordReset.txt
```

Templates use `{{placeholder}}` syntax for dynamic content:

```html
<p>Hello {{firstName}},</p>
<p>Your OTP is: {{otp}}</p>
```

## Retry Logic

The system implements exponential backoff for failed notifications:

- **Max Retries**: 5 attempts
- **Backoff Schedule**: 1 min → 5 min → 15 min → 30 min → 60 min
- **Retry Mechanism**: Azure Queue Storage visibility timeout
- **Failed Notifications**: Marked as "failed" after max retries
- **Retry Count**: Updated in database after each attempt

### Retry Flow

1. Notification fails to send
2. `retry_count` is incremented in database
3. Exception is thrown to trigger Azure Queue retry
4. Azure Queue makes message invisible for visibility timeout period
5. Message becomes visible again after timeout
6. Process repeats until success or max retries reached

### Configuration

Configure retry behavior in `host.json`:

```json
{
  "extensions": {
    "queues": {
      "maxDequeueCount": 5,
      "visibilityTimeout": "00:01:00"
    }
  }
}
```

## Database Updates

### On Successful Delivery

```sql
UPDATE notifications
SET status = 'delivered',
    delivered_at = NOW()
WHERE id = @notificationId;
```

### On Failed Delivery (After Max Retries)

```sql
UPDATE notifications
SET status = 'failed',
    retry_count = @retryCount
WHERE id = @notificationId;
```

### On Retry

```sql
UPDATE notifications
SET retry_count = @retryCount
WHERE id = @notificationId;
```

## Error Handling

### Template Not Found

- **Action**: Mark notification as failed immediately
- **Reason**: Template issue won't be fixed by retrying
- **Status**: `failed`

### Channel Directory Not Found

- **Action**: Mark notification as failed immediately
- **Reason**: Configuration issue
- **Status**: `failed`

### Send Failure (SMTP/Twilio Error)

- **Action**: Increment retry count and allow retry
- **Reason**: Transient network or service issues
- **Status**: Remains `sent` or `pending`

### Invalid Channel

- **Action**: Mark notification as failed immediately
- **Reason**: Invalid configuration
- **Status**: `failed`

## Configuration

### Local Development (local.settings.json)

For local development, configure settings in `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",

    "Smtp__Host": "smtp.example.com",
    "Smtp__Port": "587",
    "Smtp__Username": "your-smtp-username",
    "Smtp__Password": "your-smtp-password",
    "Smtp__FromEmail": "noreply@example.com",
    "Smtp__FromName": "Your Company",
    "Smtp__EnableSsl": "true",

    "Twilio__AccountSid": "your-twilio-account-sid",
    "Twilio__AuthToken": "your-twilio-auth-token",
    "Twilio__FromPhoneNumber": "+1234567890",

    "AzureQueueStorage__ConnectionString": "UseDevelopmentStorage=true",
    "AzureQueueStorage__QueueName": "notifications",

    "Database__ConnectionString": "Host=localhost;Database=notifications;Username=postgres;Password=postgres"
  }
}
```

**Note:** `local.settings.json` is automatically excluded from git and should never be committed.

### Production (Azure Application Settings + Key Vault)

In production, configure settings in Azure Portal under **Function App → Configuration → Application Settings**.

**Recommended approach using Key Vault:**

1. Store secrets in Azure Key Vault
2. Enable Managed Identity on the Function App
3. Grant Key Vault access to the Function App
4. Reference secrets using Key Vault syntax:

```
Smtp__Password=@Microsoft.KeyVault(SecretUri=https://your-kv.vault.azure.net/secrets/SmtpPassword/)
Twilio__AuthToken=@Microsoft.KeyVault(SecretUri=https://your-kv.vault.azure.net/secrets/TwilioAuthToken/)
Database__ConnectionString=@Microsoft.KeyVault(SecretUri=https://your-kv.vault.azure.net/secrets/DbConnectionString/)
```

See [CONFIGURATION.md](CONFIGURATION.md) for detailed production setup instructions.

## Testing

### Unit Tests

Run all tests:
```bash
dotnet test
```

Test coverage includes:
- ✅ Template engine rendering
- ✅ Template loading (HTML and TXT)
- ✅ Missing template handling
- ✅ Email notification processing
- ✅ SMS notification processing
- ✅ Retry logic
- ✅ Max retry exceeded
- ✅ Invalid channel handling
- ✅ Model serialization/deserialization

### Integration Testing

To test the full flow:

1. Ensure PostgreSQL is running with notifications table
2. Ensure Azurite (Azure Storage Emulator) is running: `azurite-queue`
3. Configure `local.settings.json` with valid credentials
4. Run the Functions project:
   ```bash
   cd src/NotificationProcessor.Functions
   func start
   ```
5. Add a message to the queue:
   ```bash
   # Use Azure Storage Explorer or send via SendConfigToQueueFunction
   ```

## Template Examples

### Email Template (UserWelcome.html)

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        .otp-box {
            font-size: 24px;
            font-weight: bold;
            padding: 15px;
            background: #f0f0f0;
        }
    </style>
</head>
<body>
    <h1>Welcome {{firstName}}!</h1>
    <p>Your verification code:</p>
    <div class="otp-box">{{otp}}</div>
</body>
</html>
```

### SMS Template (UserWelcome.txt)

```
Welcome {{firstName}}! Your verification code is: {{otp}}. Valid for 10 minutes.
```

## Monitoring

### Application Insights

The worker automatically logs to Application Insights:
- Notification processing started/completed
- Template loading events
- Send success/failure
- Retry events
- Error details

### Key Metrics to Monitor

1. **Queue Depth** - Number of pending messages
2. **Processing Time** - Time to process each notification
3. **Success Rate** - Percentage of successful deliveries
4. **Retry Rate** - Percentage of notifications requiring retries
5. **Failed Notifications** - Count of permanently failed notifications

### Logging Examples

```csharp
// Success
"Notification {NotificationId} processed successfully"

// Retry
"Notification {NotificationId} failed to send. Retry count: {RetryCount}"

// Failed
"Notification {NotificationId} exceeded max retries ({MaxRetries})"

// Template Not Found
"Template {Template} not found for channel {Channel}"
```

## Deployment

### Azure Functions Deployment

1. Build the project:
   ```bash
   dotnet build --configuration Release
   ```

2. Publish:
   ```bash
   cd src/NotificationProcessor.Functions
   func azure functionapp publish <your-function-app-name>
   ```

3. Configure Application Settings in Azure Portal

### Database Setup

Create the notifications table:

```sql
CREATE TABLE IF NOT EXISTS public.notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template TEXT DEFAULT null,
    channel TEXT DEFAULT null,
    retry_count INT DEFAULT 0,
    recipient TEXT DEFAULT null,
    payload JSONB DEFAULT null,
    requested_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    status VARCHAR(100) DEFAULT 'sent',
    delivered_at TIMESTAMP WITH TIME ZONE DEFAULT null
);

CREATE INDEX idx_notifications_status ON notifications(status);
CREATE INDEX idx_notifications_requested_at ON notifications(requested_at);
```

## Security Considerations

1. **Secrets Management**
   - Use Azure Key Vault for production credentials
   - Never commit credentials to source control
   - Use User Secrets for local development

2. **Email Security**
   - Enable SSL/TLS for SMTP
   - Use strong SMTP credentials
   - Validate email addresses

3. **SMS Security**
   - Rotate Twilio credentials regularly
   - Monitor usage to detect anomalies
   - Validate phone numbers

4. **Database Security**
   - Use connection string encryption
   - Implement least-privilege access
   - Enable SSL for database connections

## Troubleshooting

### Messages Not Processing

1. Check queue connection string
2. Verify queue exists and contains messages
3. Check Function App is running
4. Review Application Insights logs

### Templates Not Found

1. Verify templates are copied to output directory
2. Check template file names match exactly
3. Verify channel directory exists
4. Review file permissions

### Email/SMS Not Sending

1. Test SMTP/Twilio credentials separately
2. Check network connectivity
3. Verify recipient addresses are valid
4. Review service provider logs

### Database Updates Failing

1. Verify connection string
2. Check database is accessible
3. Verify table exists with correct schema
4. Review database logs for errors

## Performance Optimization

1. **Batch Processing**: Process multiple messages in parallel
2. **Connection Pooling**: Reuse SMTP connections
3. **Template Caching**: Cache frequently used templates
4. **Async Operations**: All I/O operations are async
5. **Singleton Services**: Services are registered as singletons

## Future Enhancements

1. **In-App Notifications**: Implement SignalR for real-time notifications
2. **Push Notifications**: Add Firebase/APNS support
3. **Template Versioning**: Support multiple template versions
4. **A/B Testing**: Template variation testing
5. **Scheduled Notifications**: Support delayed sending
6. **Rich Templates**: Support for attachments and images
7. **Analytics**: Track open rates and click-through rates
8. **Localization**: Multi-language template support

# NotificationProcessor

Azure Functions application for processing email and SMS notifications from Azure Queue Storage.

## Overview

This service is a background worker that:
1. Monitors Azure Queue Storage for notification messages
2. Loads and renders email/SMS templates with dynamic data
3. Sends notifications via SMTP (email) or Twilio (SMS)
4. Updates PostgreSQL database with delivery status
5. Implements retry logic with exponential backoff

## Architecture

This solution follows Clean Architecture principles with clear separation of concerns:

```
NotificationProcessor/
├── src/
│   ├── NotificationProcessor.Core/              # Domain models & interfaces
│   │   ├── Models/
│   │   │   ├── SmtpConfiguration.cs
│   │   │   ├── TwilioConfiguration.cs
│   │   │   ├── NotificationMessage.cs
│   │   │   ├── NotificationChannel.cs
│   │   │   └── NotificationStatus.cs
│   │   └── Interfaces/
│   │       ├── INotificationProcessor.cs
│   │       ├── ITemplateEngine.cs
│   │       ├── IEmailSender.cs
│   │       ├── ISmsSender.cs
│   │       └── INotificationRepository.cs
│   │
│   ├── NotificationProcessor.Infrastructure/     # External integrations
│   │   └── Services/
│   │       ├── NotificationProcessorService.cs   # Main processing orchestrator
│   │       ├── TemplateEngine.cs                 # Template loading & rendering
│   │       ├── EmailSender.cs                    # SMTP email sending
│   │       ├── SmsSender.cs                      # Twilio SMS sending
│   │       └── NotificationRepository.cs         # PostgreSQL data access
│   │
│   └── NotificationProcessor.Functions/          # Azure Functions
│       ├── Functions/
│       │   └── NotificationQueueWorkerFunction.cs # Queue trigger worker
│       ├── Templates/
│       │   ├── email/                            # Email templates (.html)
│       │   │   ├── UserWelcome.html
│       │   │   └── PasswordReset.html
│       │   └── sms/                              # SMS templates (.txt)
│       │       ├── UserWelcome.txt
│       │       └── PasswordReset.txt
│       ├── Program.cs                            # DI Configuration
│       ├── host.json
│       └── local.settings.json
│
└── tests/
    └── NotificationProcessor.Tests/              # NUnit tests
        ├── Models/
        │   └── NotificationMessageTests.cs
        └── Services/
            ├── TemplateEngineTests.cs
            └── NotificationProcessorServiceTests.cs
```

## Features

- **Queue-Triggered Processing**: Automatically processes notifications from Azure Queue Storage
- **Template Engine**: Dynamic template rendering with `{{placeholder}}` syntax
- **Multi-Channel Support**: Email (SMTP) and SMS (Twilio)
- **Database Integration**: PostgreSQL for delivery status tracking
- **Retry Logic**: Exponential backoff (max 5 retries)
- **Comprehensive Tests**: NUnit test coverage for all components
- **Security**: Credentials via Azure Key Vault in production
- **Monitoring**: Application Insights integration

## Queue Message Format

Send messages to the Azure Queue in this format:

```json
{
  "id": "uuid",
  "template": "forget-password",
  "channel": "email",
  "retryCount": 0,
  "recipient": "user@example.com",
  "payload": {
    "firstName": "John",
    "otp": "456789"
  },
  "requestedAt": "2026-01-14T12:00:00Z"
}
```

**Fields:**
- `id`: Notification ID (matches database record)
- `template`: Template name (e.g., "forget-password", "user-welcome")
- `channel`: "email", "sms", or "inapp"
- `retryCount`: Current retry count (start at 0)
- `recipient`: Email address (for email) or phone number (for SMS)
- `payload`: Dynamic data for template rendering
- `requestedAt`: Timestamp of request

### Template Matching

The template engine automatically selects the correct file based on the channel:

- **Email channel** → Loads `{template}.html` from `Templates/email/`
- **SMS channel** → Loads `{template}.txt` from `Templates/sms/`
- **InApp channel** → Loads `{template}.txt` from `Templates/inapp/`

**Example:**
```json
{
  "template": "forget-password",
  "channel": "email"
}
```
→ Loads `Templates/email/forget-password.html`

```json
{
  "template": "forget-password",
  "channel": "sms"
}
```
→ Loads `Templates/sms/forget-password.txt`

## Templates

Templates use `{{placeholder}}` syntax for dynamic content replacement.

### Template File Structure

Templates must be organized by channel with the correct file extension:

```
Templates/
├── email/
│   ├── forget-password.html
│   ├── user-welcome.html
│   └── order-confirmation.html
├── sms/
│   ├── forget-password.txt
│   ├── user-welcome.txt
│   └── order-confirmation.txt
└── inapp/
    └── notification.txt
```

### Example Templates

**Email Template (forget-password.html):**
```html
<!DOCTYPE html>
<html>
<body>
    <h1>Password Recovery</h1>
    <p>Hi {{firstName}},</p>
    <p>Your password reset code is: <strong>{{otp}}</strong></p>
    <p>This code expires in 10 minutes.</p>
</body>
</html>
```

**SMS Template (forget-password.txt):**
```
Hi {{firstName}}, your password reset code is {{otp}}. It expires in 10 minutes. If you didn't request this, ignore this message.
```

### Naming Conventions

- Use **kebab-case** for template names (e.g., `forget-password`, `user-welcome`)
- Email templates: **`.html`** extension
- SMS templates: **`.txt`** extension
- Template name in the queue message must match the filename (without extension)

## Configuration

### Local Development

Create `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",

    "Smtp__Host": "smtp.example.com",
    "Smtp__Port": "587",
    "Smtp__Username": "your-username",
    "Smtp__Password": "your-password",
    "Smtp__FromEmail": "noreply@example.com",
    "Smtp__FromName": "Your Company",
    "Smtp__EnableSsl": "true",

    "Twilio__AccountSid": "ACxxxxxxxx",
    "Twilio__AuthToken": "your-token",
    "Twilio__FromPhoneNumber": "+1234567890",

    "AzureQueueStorage__ConnectionString": "UseDevelopmentStorage=true",
    "AzureQueueStorage__QueueName": "notifications",

    "Database__ConnectionString": "Host=localhost;Database=notifications;Username=postgres;Password=postgres"
  }
}
```

### Production

Use Azure Application Settings with Key Vault references:

```
Smtp__Password=@Microsoft.KeyVault(SecretUri=https://your-kv.vault.azure.net/secrets/SmtpPassword/)
Twilio__AuthToken=@Microsoft.KeyVault(SecretUri=https://your-kv.vault.azure.net/secrets/TwilioAuthToken/)
Database__ConnectionString=@Microsoft.KeyVault(SecretUri=https://your-kv.vault.azure.net/secrets/DbConnectionString/)
```

See [CONFIGURATION.md](CONFIGURATION.md) for detailed setup.

## Database Schema

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
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools
- Azurite (Azure Storage Emulator)
- PostgreSQL

### Local Development

1. **Install dependencies:**
   ```bash
   dotnet restore
   ```

2. **Start Azurite:**
   ```bash
   azurite-queue
   ```

3. **Setup PostgreSQL database:**
   ```bash
   psql -d notifications -f database/migrations/001_add_delivered_at.sql
   ```

4. **Configure local.settings.json** (see Configuration section above)

5. **Run the Function App:**
   ```bash
   cd src/NotificationProcessor.Functions
   func start
   ```

### Running Tests

```bash
dotnet test
```

## Retry Logic

- **Max Retries**: 5 attempts
- **Backoff Schedule**: 1min → 5min → 15min → 30min → 60min
- **Mechanism**: Azure Queue visibility timeout
- **Failed Notifications**: Marked as "failed" after max retries

Configure in `host.json`:
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

## Deployment

```bash
# Build
dotnet build --configuration Release

# Publish to Azure
cd src/NotificationProcessor.Functions
func azure functionapp publish <your-function-app-name>

# Configure App Settings in Azure Portal
```

## Documentation

- [NOTIFICATION_WORKER.md](NOTIFICATION_WORKER.md) - Detailed worker implementation guide
- [CONFIGURATION.md](CONFIGURATION.md) - Configuration and Key Vault setup
- [ARCHITECTURE.md](ARCHITECTURE.md) - Architecture overview

## License

[Your License Here]

# NotificationProcessor

.NET Azure Functions application for processing notification messages from Azure Queue Storage. Supports email notifications via SMTP and SMS notifications via Twilio.

## Features

- **Queue-based Processing**: Automatically processes messages from Azure Queue Storage (`notifications` queue)
- **Email Notifications**: Sends emails using SMTP (MailKit) with support for both plain text and HTML content
- **SMS Notifications**: Sends SMS messages using Twilio
- **DDD Architecture**: Clean separation of concerns with Domain, Application, Infrastructure, and API layers
- **Interface-based Design**: All services use interfaces for testability and flexibility
- **Local Testing**: Supports local development and testing with Azurite
- **Scalable**: Leverages Azure Functions for automatic scaling based on queue load
- **Dependency Injection**: Uses .NET dependency injection for clean architecture

## Architecture

This project follows **Domain-Driven Design (DDD)** principles:

```
Azure Queue Storage (notifications)
         ↓
┌────────────────────────────────────────┐
│      API Layer (Azure Functions)       │
│   - NotificationQueueTrigger          │
└────────────────┬───────────────────────┘
                 ↓
┌────────────────────────────────────────┐
│     Application Layer (Services)       │
│   - INotificationService              │
│   - NotificationService               │
└────────────────┬───────────────────────┘
                 ↓
┌────────────────────────────────────────┐
│      Domain Layer (Entities)           │
│   - NotificationMessage               │
│   - EmailNotification                 │
│   - SmsNotification                   │
│   - IEmailProvider / ISmsProvider     │
└────────────────┬───────────────────────┘
                 ↓
┌────────────────────────────────────────┐
│  Infrastructure Layer (Providers)      │
│   - SmtpEmailProvider (MailKit)       │
│   - TwilioSmsProvider (Twilio)        │
└────────────────────────────────────────┘
```

**Benefits of this architecture**:
- Clear separation of concerns
- Easy to test (mock interfaces)
- Flexible (swap implementations without changing business logic)
- Maintainable (each layer has a single responsibility)
- Extensible (add new notification types without major changes)

## Prerequisites

- .NET 6.0 SDK or higher
- Azure Functions Core Tools v4.x
- Azure Storage Account (or Azurite for local testing)
- SMTP server credentials (e.g., Gmail, SendGrid, Office 365)
- Twilio account (for SMS functionality)

## Installation

1. **Clone the repository**
   ```bash
   cd NotificationProcessor
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Install Azure Functions Core Tools**
   - Windows: `npm install -g azure-functions-core-tools@4`
   - macOS: `brew tap azure/functions && brew install azure-functions-core-tools@4`
   - Linux: Follow instructions at https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local

4. **Configure environment variables**
   - Copy `.env.example` to `.env` and fill in your values
   - Or update `local.settings.json` directly

## Configuration

### Azure Storage Connection

Update `local.settings.json` with your Azure Storage connection string:

```json
{
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=clereviewst;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
    "AZURE_STORAGE_CONNECTION_STRING": "DefaultEndpointsProtocol=https;AccountName=clereviewst;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
    "QUEUE_NAME": "notifications"
  }
}
```

**Queue URL**: `https://clereviewst.queue.core.windows.net/notifications`

### SMTP Configuration (Email)

Configure your SMTP settings in `local.settings.json`:

```json
{
  "Values": {
    "SMTP_HOST": "smtp.gmail.com",
    "SMTP_PORT": "587",
    "SMTP_USER": "your-email@example.com",
    "SMTP_PASSWORD": "your-app-password",
    "SMTP_FROM_EMAIL": "your-email@example.com"
  }
}
```

**Gmail Users**: Use an [App Password](https://support.google.com/accounts/answer/185833) instead of your regular password.

**Other SMTP Providers**:
- **Office 365**: `smtp.office365.com:587`
- **SendGrid**: `smtp.sendgrid.net:587`
- **Amazon SES**: `email-smtp.us-east-1.amazonaws.com:587`

### Twilio Configuration (SMS)

Configure your Twilio credentials in `local.settings.json`:

```json
{
  "Values": {
    "TWILIO_ACCOUNT_SID": "your-account-sid",
    "TWILIO_AUTH_TOKEN": "your-auth-token",
    "TWILIO_FROM_PHONE": "+1234567890"
  }
}
```

Get your credentials from: https://console.twilio.com/

## Message Format

### Email Notification

```json
{
  "type": "email",
  "data": {
    "to": "recipient@example.com",
    "subject": "Your Subject Here",
    "body": "Plain text body content",
    "html": "<html><body><h1>HTML Content</h1></body></html>"
  }
}
```

**Fields**:
- `to` (required): Recipient email address
- `subject` (optional): Email subject line (default: "Notification")
- `body` (required): Plain text email body
- `html` (optional): HTML version of email body

### SMS Notification

```json
{
  "type": "sms",
  "data": {
    "to": "+1234567890",
    "body": "Your SMS message content here"
  }
}
```

**Fields**:
- `to` (required): Recipient phone number in E.164 format (e.g., +1234567890)
- `body` (required): SMS message content (max 1600 characters)

## Local Development

### Option 1: Using Azurite (Local Storage Emulator)

1. **Install and start Azurite**
   ```bash
   npm install -g azurite
   azurite --silent --location ./azurite --debug ./azurite/debug.log
   ```

2. **Update connection string in `local.settings.json`**
   ```json
   {
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "AZURE_STORAGE_CONNECTION_STRING": "UseDevelopmentStorage=true"
     }
   }
   ```

3. **Build and start the Azure Function**
   ```bash
   dotnet build
   func start
   ```

### Option 2: Using Azure Storage Account

1. **Update `local.settings.json` with your Azure Storage connection string**

2. **Build and start the Azure Function**
   ```bash
   dotnet build
   func start
   ```

The function will be available at `http://localhost:7071`

## Testing

### Using the Test Scripts

**Linux/macOS** (requires Azure CLI):
```bash
chmod +x test-queue.sh
./test-queue.sh
```

**Windows PowerShell**:
```powershell
.\test-queue.ps1
```

### Manual Testing with Azure CLI

```bash
# Send email notification
az storage message put \
  --queue-name notifications \
  --content @TestMessages/email-test.json \
  --connection-string "YOUR_CONNECTION_STRING"

# Send SMS notification
az storage message put \
  --queue-name notifications \
  --content @TestMessages/sms-test.json \
  --connection-string "YOUR_CONNECTION_STRING"
```

### Using Azure Storage Explorer

1. Download [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/)
2. Connect to your storage account
3. Navigate to Queues → notifications
4. Click "Add Message"
5. Paste the JSON content from `TestMessages/email-test.json` or `TestMessages/sms-test.json`
6. Click OK

### Using C# Code

```csharp
using Azure.Storage.Queues;

var connectionString = "YOUR_CONNECTION_STRING";
var queueClient = new QueueClient(connectionString, "notifications");

// Send email notification
var emailMessage = @"{
    ""type"": ""email"",
    ""data"": {
        ""to"": ""recipient@example.com"",
        ""subject"": ""Test"",
        ""body"": ""Test email""
    }
}";
await queueClient.SendMessageAsync(emailMessage);

// Send SMS notification
var smsMessage = @"{
    ""type"": ""sms"",
    ""data"": {
        ""to"": ""+1234567890"",
        ""body"": ""Test SMS""
    }
}";
await queueClient.SendMessageAsync(smsMessage);
```

## Build and Run

### Build the project
```bash
dotnet build
```

### Run locally
```bash
func start
# or
dotnet run
```

### Run with watch mode (auto-reload on changes)
```bash
func start --dotnet-isolated-debug
```

## Deployment

### Deploy to Azure using Azure Functions Core Tools

1. **Create Azure Function App** (if not exists)
   ```bash
   az functionapp create \
     --resource-group your-resource-group \
     --consumption-plan-location eastus \
     --runtime dotnet-isolated \
     --runtime-version 6 \
     --functions-version 4 \
     --name your-function-app-name \
     --storage-account clereviewst
   ```

2. **Configure Application Settings**
   ```bash
   az functionapp config appsettings set \
     --name your-function-app-name \
     --resource-group your-resource-group \
     --settings \
       AZURE_STORAGE_CONNECTION_STRING="your-connection-string" \
       SMTP_HOST="smtp.gmail.com" \
       SMTP_PORT="587" \
       SMTP_USER="your-email@example.com" \
       SMTP_PASSWORD="your-password" \
       SMTP_FROM_EMAIL="your-email@example.com" \
       TWILIO_ACCOUNT_SID="your-sid" \
       TWILIO_AUTH_TOKEN="your-token" \
       TWILIO_FROM_PHONE="+1234567890"
   ```

3. **Publish the function**
   ```bash
   func azure functionapp publish your-function-app-name
   ```

### Deploy using Visual Studio

1. Right-click the project → Publish
2. Select Azure → Azure Function App (Windows or Linux)
3. Select or create a Function App
4. Publish

### Deploy using GitHub Actions

See `.github/workflows/deploy.yml` for CI/CD pipeline example.

## Project Structure (DDD Architecture)

The project follows Domain-Driven Design (DDD) principles with clear separation of concerns:

```
NotificationProcessor/
├── API/                                    # API Layer (Azure Functions endpoints)
│   └── Functions/
│       └── NotificationQueueTrigger.cs    # Queue trigger function
│
├── Application/                           # Application Layer (Business logic & services)
│   ├── Interfaces/
│   │   └── INotificationService.cs       # Notification service interface
│   └── Services/
│       └── NotificationService.cs        # Notification service implementation
│
├── Domain/                                # Domain Layer (Core business entities & rules)
│   ├── Entities/
│   │   ├── NotificationMessage.cs        # Base notification entity
│   │   ├── EmailNotification.cs          # Email notification entity
│   │   └── SmsNotification.cs            # SMS notification entity
│   └── Interfaces/
│       ├── IEmailProvider.cs             # Email provider interface
│       └── ISmsProvider.cs               # SMS provider interface
│
├── Infrastructure/                        # Infrastructure Layer (External services & DB)
│   └── Providers/
│       ├── SmtpEmailProvider.cs          # SMTP email implementation
│       └── TwilioSmsProvider.cs          # Twilio SMS implementation
│
├── TestMessages/
│   ├── email-test.json                   # Email test message template
│   └── sms-test.json                     # SMS test message template
│
├── Program.cs                            # Dependency injection configuration
├── host.json                             # Azure Functions host config
├── local.settings.json                   # Local configuration (not in git)
├── NotificationProcessor.csproj          # Project file
├── .env.example                          # Environment variable template
├── .gitignore                            # Git ignore rules
├── test-queue.sh                         # Bash test script
├── test-queue.ps1                        # PowerShell test script
└── README.md                             # This file
```

### Layer Responsibilities

**API Layer**: Handles incoming requests (queue triggers, HTTP endpoints). Thin layer that delegates to Application services.

**Application Layer**: Contains business logic and orchestration. Coordinates between domain entities and infrastructure.

**Domain Layer**: Core business entities, value objects, and domain interfaces. Framework-agnostic and contains no external dependencies.

**Infrastructure Layer**: Implements domain interfaces with concrete external services (SMTP, Twilio, databases, etc.).

## NuGet Packages Used

- **Microsoft.NET.Sdk.Functions** (4.2.0) - Azure Functions SDK
- **Microsoft.Azure.Functions.Worker** (1.19.0) - Isolated worker runtime
- **Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues** (5.2.0) - Queue trigger bindings
- **Azure.Storage.Queues** (12.17.1) - Azure Queue Storage client
- **MailKit** (4.3.0) - SMTP email client
- **Twilio** (6.16.1) - Twilio SMS client

## Monitoring

### View Logs Locally

Logs are displayed in the console when running `func start`

### View Logs in Azure

```bash
# Stream logs
func azure functionapp logstream your-function-app-name

# Or use Azure CLI
az webapp log tail --name your-function-app-name --resource-group your-resource-group
```

### Application Insights

Enable Application Insights in Azure Portal for:
- Request tracking
- Dependency tracking
- Exception tracking
- Custom metrics and logs

### Queue Metrics

Monitor in Azure Portal:
- Queue message count
- Message processing rate
- Failed messages (poison queue: `notifications-poison`)

## Error Handling

- **Invalid JSON**: Logged and message removed from queue
- **Missing configuration**: Logged as error, message removed
- **Failed sends**: Logged with full exception details
- **Retry logic**: Azure Functions automatically retries failed messages (5 times by default)
- **Poison queue**: Messages that fail repeatedly are moved to `notifications-poison` queue

## Security Best Practices

- ✅ Never commit `local.settings.json` to version control
- ✅ Use Azure Key Vault for production credentials
- ✅ Enable Managed Identity for Azure resources
- ✅ Rotate API keys and passwords regularly
- ✅ Use HTTPS only in production
- ✅ Implement rate limiting for high-volume scenarios
- ✅ Validate and sanitize all input data
- ✅ Use App Passwords for Gmail (not account passwords)

## Troubleshooting

### Function not triggering

- ✓ Check Azure Storage connection string is correct
- ✓ Verify queue name is "notifications"
- ✓ Ensure Azure Functions Core Tools is running
- ✓ Check if messages are in the queue
- ✓ Review function logs for errors

### Build errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Email not sending

- ✓ Verify SMTP credentials are correct
- ✓ For Gmail, use App Password (not regular password)
- ✓ Check SMTP port (587 for TLS, 465 for SSL)
- ✓ Verify firewall allows outbound SMTP connections
- ✓ Check email provider rate limits

### SMS not sending

- ✓ Verify Twilio Account SID and Auth Token
- ✓ Ensure phone numbers are in E.164 format (+1234567890)
- ✓ Check Twilio account balance
- ✓ Verify "from" number is a valid Twilio phone number
- ✓ Check phone number is not blocked/blacklisted

### Local testing with Azurite

```bash
# Start Azurite with verbose logging
azurite --silent=false --location ./azurite --debug ./azurite/debug.log

# Create the queue manually
az storage queue create --name notifications --connection-string "UseDevelopmentStorage=true"
```

## Performance Considerations

- **Batch processing**: Consider batch dequeue for high-volume scenarios
- **Visibility timeout**: Default is 30 seconds, adjust if processing takes longer
- **Max dequeue count**: Default is 5, messages move to poison queue after
- **Scaling**: Function scales automatically based on queue length

## License

MIT

## Support

For issues and questions, please open an issue in the repository.

---

**Queue URL**: `https://clereviewst.queue.core.windows.net/notifications`
**Runtime**: .NET 6.0 (Isolated Worker)
**Handlers**: Email (SMTP/MailKit) | SMS (Twilio)

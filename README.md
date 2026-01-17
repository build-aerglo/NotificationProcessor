# NotificationProcessor

Azure Functions .NET application for managing notification service credentials (SMTP and Twilio) and exposing them to Azure Queue Storage.

## Architecture

This solution follows Clean Architecture principles with clear separation of concerns:

```
NotificationProcessor/
├── src/
│   ├── NotificationProcessor.Core/           # Domain models & interfaces
│   │   ├── Models/                            # DTOs and domain models
│   │   │   ├── SmtpConfiguration.cs
│   │   │   ├── TwilioConfiguration.cs
│   │   │   ├── NotificationConfigRequest.cs
│   │   │   └── NotificationConfigResponse.cs
│   │   └── Interfaces/                        # Service contracts
│   │       ├── INotificationConfigService.cs
│   │       └── IQueueService.cs
│   │
│   ├── NotificationProcessor.Infrastructure/  # External integrations
│   │   └── Services/
│   │       ├── NotificationConfigService.cs   # Configuration management
│   │       └── QueueService.cs                # Azure Queue Storage
│   │
│   └── NotificationProcessor.Functions/       # Azure Functions
│       ├── Functions/
│       │   ├── GetSmtpConfigFunction.cs       # GET /api/config/smtp
│       │   ├── GetTwilioConfigFunction.cs     # GET /api/config/twilio
│       │   └── SendConfigToQueueFunction.cs   # POST /api/config/queue
│       ├── Program.cs                         # DI Configuration
│       ├── host.json
│       └── local.settings.json
│
└── tests/
    └── NotificationProcessor.Tests/           # NUnit tests
        ├── Models/
        │   └── ModelTests.cs
        └── Services/
            └── NotificationConfigServiceTests.cs
```

## Features

- **SMTP Configuration Management**: Securely store and retrieve email notification credentials
- **Twilio Configuration Management**: Securely store and retrieve SMS notification credentials
- **Azure Queue Integration**: Send configuration to Azure Queue Storage for consumption by other services
- **RESTful API**: HTTP-triggered Azure Functions with clean endpoints
- **Dependency Injection**: Proper DI setup following Azure Functions best practices
- **Unit Tests**: Comprehensive NUnit test coverage
- **Security Updates**: All packages updated to address CVE-2024-43485 and other vulnerabilities

## Important: Service Responsibility

**This service ONLY provides credentials** - it does NOT handle:
- Template loading or HTML generation
- Message formatting
- Actual email/SMS sending

**Your Notification API should**:
- Receive notification requests with template data
- Load and merge HTML/text templates with data
- Call this service to get SMTP/Twilio credentials
- Send emails/SMS using those credentials

See [INTEGRATION.md](./INTEGRATION.md) for detailed integration patterns and examples.

## API Endpoints

### 1. Get SMTP Configuration
```http
GET /api/config/smtp
Authorization: Function key required
```

**Response:**
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

### 2. Get Twilio Configuration
```http
GET /api/config/twilio
Authorization: Function key required
```

**Response:**
```json
{
  "accountSid": "AC1234567890",
  "authToken": "your-auth-token",
  "fromPhoneNumber": "+1234567890"
}
```

### 3. Send Configuration to Queue
```http
POST /api/config/queue
Authorization: Function key required
Content-Type: application/json
```

**Request Body:**
```json
{
  "notificationType": "email"  // or "sms", "smtp", "twilio"
}
```

**Response:**
```json
{
  "message": "Configuration sent to queue successfully",
  "requestId": "guid",
  "notificationType": "email"
}
```

## Configuration

### Local Development

Update `src/NotificationProcessor.Functions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "your-smtp-username",
    "Password": "your-smtp-password",
    "FromEmail": "noreply@example.com",
    "FromName": "Notification System",
    "EnableSsl": true
  },
  "Twilio": {
    "AccountSid": "your-twilio-account-sid",
    "AuthToken": "your-twilio-auth-token",
    "FromPhoneNumber": "+1234567890"
  },
  "AzureQueueStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=clereviewst;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=core.windows.net",
    "QueueName": "notifications"
  }
}
```

### Azure Deployment

Set the following Application Settings in Azure Portal:

| Setting | Description |
|---------|-------------|
| `Smtp__Host` | SMTP server hostname |
| `Smtp__Port` | SMTP server port (usually 587) |
| `Smtp__Username` | SMTP username |
| `Smtp__Password` | SMTP password |
| `Smtp__FromEmail` | Default sender email |
| `Smtp__FromName` | Default sender name |
| `Smtp__EnableSsl` | Enable SSL/TLS (true/false) |
| `Twilio__AccountSid` | Twilio Account SID |
| `Twilio__AuthToken` | Twilio Auth Token |
| `Twilio__FromPhoneNumber` | Twilio phone number |
| `AzureQueueStorage__ConnectionString` | Azure Storage connection string |
| `AzureQueueStorage__QueueName` | Queue name (default: "notifications") |

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- Azure Functions Core Tools (for local development)
- Azure Storage Emulator or Azure Storage Account

### Package Versions

All packages have been updated to latest stable versions to address security vulnerabilities:

**Azure Functions**:
- Microsoft.Azure.Functions.Worker 1.23.0
- Microsoft.Azure.Functions.Worker.Extensions.Http 3.2.0
- Microsoft.Azure.Functions.Worker.Sdk 1.17.4

**Security Fixes**:
- System.Text.Json 8.0.5 (fixes CVE-2024-43485)
- coverlet.collector 6.0.2 (fixes vulnerabilities reported by mend.io)
- Azure.Storage.Queues 12.21.0 (latest stable)

### Build
```bash
dotnet build NotificationProcessor.sln
```

### Run Locally
```bash
cd src/NotificationProcessor.Functions
func start
```

### Run Tests
```bash
dotnet test NotificationProcessor.sln
```

## Azure Queue Storage

The application connects to Azure Queue Storage at:
```
https://clereviewst.queue.core.windows.net/notifications
```

Messages sent to the queue are in JSON format:
```json
{
  "requestId": "guid",
  "notificationType": "email",
  "configuration": {
    "host": "smtp.example.com",
    "port": 587,
    ...
  },
  "success": true,
  "timestamp": "2024-01-17T12:00:00Z"
}
```

## Security Considerations

1. **Never commit** `local.settings.json` with real credentials
2. Use **Azure Key Vault** for production credentials
3. Enable **Function-level authorization** for all endpoints
4. Use **Managed Identity** for Azure Queue Storage authentication (recommended)
5. Rotate credentials regularly
6. Enable **Application Insights** for monitoring and diagnostics

## Integration with Notification API

This service is designed to work alongside:
- **Notification API**: Consumes configurations to send notifications
- **Azure Queue Storage**: Acts as a message broker between services

Typical flow:
1. Notification API requests configuration via HTTP endpoint
2. This service retrieves configuration from secure storage
3. Configuration is sent to Azure Queue for processing
4. Notification API consumes from queue and sends notifications

## Development

### Adding New Configuration Types

1. Create model in `Core/Models/`
2. Add interface method in `INotificationConfigService`
3. Implement in `NotificationConfigService`
4. Create Azure Function endpoint
5. Add configuration section to `appsettings.json`
6. Write unit tests

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "FullyQualifiedName~NotificationConfigServiceTests"
```

## Troubleshooting

### Common Issues

1. **Queue not found**: Ensure the queue exists in Azure Storage or run locally with Azurite
2. **Configuration null**: Check that settings are properly configured in `local.settings.json`
3. **Authentication errors**: Verify connection strings and credentials

## License

MIT License

## Support

For issues and questions, please open an issue in the repository
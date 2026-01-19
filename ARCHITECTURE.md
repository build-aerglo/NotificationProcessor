# Architecture Documentation

## Overview

The NotificationProcessor is an Azure Functions application that processes notification messages from Azure Queue Storage. It follows Clean Architecture principles to ensure separation of concerns, testability, and maintainability.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Azure Queue Storage                     │
│                    (Notification Messages)                   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓ Queue Trigger
┌─────────────────────────────────────────────────────────────┐
│           NotificationQueueWorkerFunction                    │
│                  (Azure Functions)                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│           NotificationProcessorService                       │
│              (Orchestrates Processing)                       │
└─────┬──────────────┬──────────────┬──────────────┬─────────┘
      │              │              │              │
      ↓              ↓              ↓              ↓
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐
│Template  │  │  Email   │  │   SMS    │  │ Notification │
│ Engine   │  │  Sender  │  │  Sender  │  │  Repository  │
└──────────┘  └──────────┘  └──────────┘  └──────────────┘
      │              │              │              │
      ↓              ↓              ↓              ↓
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐
│Templates │  │  Azure   │  │  Twilio  │  │  PostgreSQL  │
│  Files   │  │Comm Svc  │  │   API    │  │   Database   │
└──────────┘  └──────────┘  └──────────┘  └──────────────┘
```

## Layers

### 1. Core Layer (`NotificationProcessor.Core`)

**Purpose**: Contains business logic, domain models, and service interfaces.

**Dependencies**: None (this layer is completely independent)

**Contents**:

**Models**:
- `NotificationMessage`: Queue message structure
- `NotificationChannel`: Channel constants (email, sms, inapp)
- `NotificationStatus`: Status constants (pending, sent, delivered, failed)
- `AzureEmailConfiguration`: Azure Communication Services email credentials
- `TwilioConfiguration`: SMS service credentials

**Interfaces**:
- `INotificationProcessor`: Main processing orchestrator
- `ITemplateEngine`: Template loading and rendering
- `IEmailSender`: Email sending abstraction
- `ISmsSender`: SMS sending abstraction
- `INotificationRepository`: Database operations

**Design Principles**:
- No external dependencies (pure .NET)
- Interface-based contracts for all services
- Immutable domain models
- Single Responsibility Principle

### 2. Infrastructure Layer (`NotificationProcessor.Infrastructure`)

**Purpose**: Implements infrastructure concerns and external integrations.

**Dependencies**:
- NotificationProcessor.Core (interfaces)
- Azure.Storage.Queues (queue operations)
- Npgsql (PostgreSQL)
- Twilio (SMS)
- Microsoft.Extensions.* (configuration, logging)

**Services**:

**NotificationProcessorService**:
- Orchestrates notification processing workflow
- Handles retry logic
- Updates database status
- Error handling and logging

**TemplateEngine**:
- Loads templates from file system
- Supports .html (email) and .txt (sms) files
- Renders templates with `{{placeholder}}` replacement
- Organizes templates by channel (email/, sms/)

**EmailSender**:
- Sends emails via Azure Communication Services
- HTML support
- Configurable sender address
- Built-in Azure reliability and delivery tracking

**SmsSender**:
- Sends SMS via Twilio API
- Phone number validation
- Delivery status tracking

**NotificationRepository**:
- PostgreSQL integration
- Updates delivery status
- Tracks retry counts
- Records delivered timestamp

**Design Principles**:
- Implements Core interfaces
- Singleton service lifetime
- Async/await throughout
- Comprehensive error handling
- Structured logging with ILogger

### 3. Functions Layer (`NotificationProcessor.Functions`)

**Purpose**: Azure Functions entry point and queue trigger.

**Dependencies**:
- NotificationProcessor.Core
- NotificationProcessor.Infrastructure
- Microsoft.Azure.Functions.Worker (isolated process)
- Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues

**Contents**:

**Functions**:
- `NotificationQueueWorkerFunction`: Queue-triggered background worker

**Templates**:
```
Templates/
├── email/
│   ├── UserWelcome.html
│   └── PasswordReset.html
└── sms/
    ├── UserWelcome.txt
    └── PasswordReset.txt
```

**Program.cs**:
- Dependency injection configuration
- Service registration
- Configuration binding (AzureEmail, Twilio, Database)
- Template path setup

**Design Principles**:
- Isolated process model (.NET 8)
- Stateless function execution
- Dependency injection for all services
- Configuration via environment variables
- Templates deployed with application

### 4. Tests Layer (`NotificationProcessor.Tests`)

**Purpose**: Unit and integration tests.

**Dependencies**:
- All project layers
- NUnit (test framework)
- Moq (mocking)
- coverlet.collector (code coverage)

**Test Coverage**:

**TemplateEngineTests**:
- Placeholder rendering
- Template loading (HTML/TXT)
- Missing template handling
- Empty template scenarios

**NotificationProcessorServiceTests**:
- Email notification processing
- SMS notification processing
- Retry logic
- Max retries exceeded
- Invalid channel handling
- Template not found scenarios

**NotificationMessageTests**:
- Model serialization/deserialization
- Default values
- JSON compatibility

**Design Principles**:
- Arrange-Act-Assert pattern
- Mock external dependencies
- In-memory configuration
- Isolated test execution

## Data Flow

### Successful Notification Flow

```
1. Queue Message Received
   ↓
2. Deserialize to NotificationMessage
   ↓
3. Check retry count < max (5)
   ↓
4. Load template from Templates/{channel}/{template}.{ext}
   ↓
5. Render template with payload data
   ↓
6. Send via appropriate channel (Azure Communication Services/Twilio)
   ↓
7. Update database: status = 'delivered', delivered_at = NOW()
   ↓
8. Complete (message removed from queue)
```

### Failed Notification Flow

```
1. Queue Message Received
   ↓
2. Deserialize to NotificationMessage
   ↓
3. Check retry count < max (5)
   ↓
4. Attempt processing...
   ↓
5. Send fails (Azure Communication Services error, network issue, etc.)
   ↓
6. Increment retry_count in database
   ↓
7. Throw exception
   ↓
8. Azure Queue makes message invisible (visibility timeout)
   ↓
9. After timeout, message becomes visible again
   ↓
10. Repeat from step 1 (with incremented retry_count)
```

### Max Retries Exceeded Flow

```
1. Queue Message Received (retry_count = 5)
   ↓
2. Check retry count >= max
   ↓
3. Mark as failed in database
   ↓
4. Complete (message removed from queue)
   ↓
5. Message moved to poison queue (by Azure)
```

## Configuration Architecture

### Local Development
- **Source**: `local.settings.json`
- **Pattern**: Flat key-value with double underscore hierarchy
- **Secrets**: Plain text (never committed)
- **Storage**: Azurite (local emulator)

### Production (Azure)
- **Source**: Application Settings
- **Pattern**: Key Vault references for secrets
- **Secrets**: Azure Key Vault with Managed Identity
- **Storage**: Azure Queue Storage

**Configuration Flow**:
```
IConfiguration (built by host)
    ↓
Environment variables (Azure App Settings)
    ↓
Key Vault resolution (if using @Microsoft.KeyVault syntax)
    ↓
Bound to typed configuration objects
    ↓
Injected into services
```

## Scalability & Performance

### Horizontal Scaling
- **Stateless design**: No in-memory state
- **Singleton services**: Shared across function invocations
- **Connection pooling**: Azure Communication Services client and database connections reused
- **Parallel processing**: Multiple queue messages processed concurrently

### Retry Strategy
- **Exponential backoff**: 1min → 5min → 15min → 30min → 60min
- **Max retries**: 5 attempts
- **Visibility timeout**: Azure Queue handles automatic retry
- **Poison queue**: Failed messages after max retries

### Monitoring
- **Application Insights**: Automatic telemetry
- **Structured logging**: ILogger throughout
- **Key metrics**:
  - Queue depth
  - Processing time
  - Success/failure rates
  - Retry counts

## Security Architecture

### Secrets Management
- **Local**: `local.settings.json` (gitignored)
- **Production**: Azure Key Vault
- **Access**: Managed Identity (no credentials in code)

### Authentication
- **Queue access**: Connection string (via Key Vault)
- **Database**: PostgreSQL connection string (via Key Vault)
- **Azure Communication Services**: Connection string with access key (via Key Vault)
- **Twilio**: Account SID + Auth Token (via Key Vault)

### Data Protection
- **In transit**: SSL/TLS for all connections
- **At rest**: Azure Storage encryption, PostgreSQL encryption
- **Secrets**: Never logged or exposed in responses

## Extensibility Points

### Adding New Channels
1. Add channel constant in `NotificationChannel.cs`
2. Create sender interface in Core (`INewChannelSender`)
3. Implement sender in Infrastructure
4. Register in `Program.cs`
5. Update `NotificationProcessorService` switch statement
6. Create template directory `Templates/newchannel/`

### Adding New Templates
1. Create template file in `Templates/{channel}/{TemplateName}.{ext}`
2. Use `{{placeholder}}` syntax
3. Deploy with application (automatically copied)

### Custom Template Rendering
- Implement `ITemplateEngine` with custom logic
- Register in DI container
- Supports advanced scenarios (Razor, Liquid, etc.)

## Design Patterns Used

- **Dependency Injection**: Constructor injection throughout
- **Repository Pattern**: `INotificationRepository` abstracts data access
- **Strategy Pattern**: Different senders for different channels
- **Template Method**: Base processing flow with channel-specific steps
- **Factory Pattern**: Service creation via DI container
- **Single Responsibility**: Each class has one reason to change

## Technology Stack

- **.NET 8.0**: LTS runtime
- **Azure Functions v4**: Isolated process model
- **Azure Queue Storage**: Message queuing
- **PostgreSQL**: Relational database
- **Azure Communication Services**: Email delivery
- **Twilio**: SMS delivery
- **NUnit**: Testing framework
- **Moq**: Mocking framework

## Future Enhancements

1. **In-App Notifications**: SignalR integration
2. **Push Notifications**: Firebase/APNS support
3. **Template Versioning**: A/B testing support
4. **Rich Media**: Attachment support for emails
5. **Analytics**: Open/click tracking
6. **Localization**: Multi-language templates
7. **Scheduled Sending**: Delayed notifications
8. **Priority Queues**: High/low priority messages

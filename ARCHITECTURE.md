# Architecture Documentation

## Overview

The NotificationProcessor application follows Clean Architecture principles to ensure separation of concerns, testability, and maintainability.

## Layers

### 1. Core Layer (`NotificationProcessor.Core`)

**Purpose**: Contains business logic, domain models, and interfaces.

**Dependencies**: None (this layer is independent)

**Contents**:
- **Models**: DTOs and domain objects
  - `SmtpConfiguration`: Email service credentials
  - `TwilioConfiguration`: SMS service credentials
  - `NotificationConfigRequest`: Request model
  - `NotificationConfigResponse`: Response model

- **Interfaces**: Service contracts
  - `INotificationConfigService`: Configuration retrieval
  - `IQueueService`: Queue operations

**Design Principles**:
- No external dependencies
- Pure domain logic
- Interface-based contracts

### 2. Infrastructure Layer (`NotificationProcessor.Infrastructure`)

**Purpose**: Implements infrastructure concerns and external integrations.

**Dependencies**:
- NotificationProcessor.Core
- Azure.Storage.Queues
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging

**Contents**:
- **Services**:
  - `NotificationConfigService`: Implements configuration retrieval from app settings
  - `QueueService`: Implements Azure Queue Storage operations

**Design Principles**:
- Implements Core interfaces
- Handles external service integration
- Manages configuration binding

### 3. Functions Layer (`NotificationProcessor.Functions`)

**Purpose**: Azure Functions HTTP endpoints and application entry point.

**Dependencies**:
- NotificationProcessor.Core
- NotificationProcessor.Infrastructure
- Microsoft.Azure.Functions.Worker

**Contents**:
- **Functions**:
  - `GetSmtpConfigFunction`: GET /api/config/smtp
  - `GetTwilioConfigFunction`: GET /api/config/twilio
  - `SendConfigToQueueFunction`: POST /api/config/queue
- **Program.cs**: Dependency injection configuration

**Design Principles**:
- Thin controllers
- Dependency injection
- HTTP protocol handling only

### 4. Tests Layer (`NotificationProcessor.Tests`)

**Purpose**: Unit and integration tests.

**Dependencies**:
- All project layers
- NUnit
- Moq

**Contents**:
- **Models**: Model validation tests
- **Services**: Service behavior tests

## Data Flow

### GET Configuration Flow

```
HTTP Request → Function → ConfigService → Configuration → Response
```

1. Client sends HTTP GET to `/api/config/smtp` or `/api/config/twilio`
2. Azure Function receives request
3. Function calls `INotificationConfigService`
4. Service retrieves configuration from app settings
5. Configuration returned as JSON

### POST to Queue Flow

```
HTTP Request → Function → ConfigService → QueueService → Azure Queue
```

1. Client sends HTTP POST to `/api/config/queue` with notification type
2. Azure Function receives and validates request
3. Function calls `INotificationConfigService.GetConfiguration()`
4. Service retrieves appropriate configuration
5. Function calls `IQueueService.SendConfigurationAsync()`
6. QueueService serializes and sends to Azure Queue
7. Success response returned

## Dependency Injection

```csharp
services.AddScoped<INotificationConfigService, NotificationConfigService>();
services.AddScoped<IQueueService, QueueService>();
```

**Scoped Lifetime**: New instance per HTTP request, ensuring proper isolation.

## Configuration Management

Configuration follows the ASP.NET Core configuration pattern:

- Local development: `local.settings.json`
- Azure deployment: Application Settings (environment variables)
- Hierarchical binding using `IConfiguration.GetSection()`

## Security Architecture

1. **Authentication**: Function-level authorization keys
2. **Secrets Management**:
   - Local: `local.settings.json` (git-ignored)
   - Production: Azure App Settings or Key Vault
3. **Queue Access**: Connection strings with SAS tokens or Managed Identity

## Extension Points

To add a new notification provider:

1. Create model in `Core/Models/`
2. Add method to `INotificationConfigService`
3. Implement in `NotificationConfigService`
4. Create new Function endpoint
5. Add configuration section
6. Write tests

## Testing Strategy

- **Unit Tests**: Test business logic in isolation using mocks
- **Integration Tests**: Test Azure Functions with test configuration
- **Mocking**: Moq for interface mocking
- **Configuration**: In-memory configuration for tests

## Scalability Considerations

1. **Stateless Design**: All functions are stateless
2. **Queue Pattern**: Async processing via Azure Queue
3. **Horizontal Scaling**: Functions can scale independently
4. **Connection Pooling**: Azure SDK handles connection management

## Monitoring and Observability

- **Application Insights**: Built-in telemetry
- **Structured Logging**: ILogger throughout
- **Correlation IDs**: RequestId tracking in responses

## Future Enhancements

1. **Azure Key Vault Integration**: Enhanced secret management
2. **Managed Identity**: Remove connection strings
3. **Cache Layer**: Redis for configuration caching
4. **Rate Limiting**: Protect endpoints from abuse
5. **Health Checks**: Endpoint for monitoring

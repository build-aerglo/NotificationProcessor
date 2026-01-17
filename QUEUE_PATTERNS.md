# Azure Queue Storage Integration Patterns

## Overview

Azure Queue Storage (`https://clereviewst.queue.core.windows.net/notifications`) can be used in different ways depending on your architecture needs. Here are the three main patterns:

---

## Pattern 1: Queue for Notification Requests (RECOMMENDED)

**What goes in the queue**: Notification requests from external systems
**Who puts it there**: Your application/users/systems
**Who consumes it**: Notification API
**Credentials from**: NotificationProcessor via HTTP

### Architecture
```
External System/User
    ↓ (sends notification request)
Azure Queue Storage
    ↓ (consumes messages)
Notification API
    ↓ (HTTP: GET /api/config/smtp or /api/config/twilio)
NotificationProcessor (this service)
    ↓ (returns credentials)
Notification API
    ↓ (processes template, sends email/sms)
SMTP/Twilio
```

### Queue Message Format
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

### Notification API Implementation
```csharp
public class QueueConsumerService : BackgroundService
{
    private readonly QueueClient _queueClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailService _emailService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 1. Get messages from queue
            QueueMessage[] messages = await _queueClient
                .ReceiveMessagesAsync(maxMessages: 10);

            foreach (var message in messages)
            {
                var notification = JsonSerializer
                    .Deserialize<NotificationRequest>(message.Body);

                // 2. Get credentials from NotificationProcessor
                var smtpConfig = await GetSmtpCredentials();

                // 3. Process template and send
                await _emailService.SendAsync(smtpConfig, notification);

                // 4. Delete message from queue on success
                await _queueClient.DeleteMessageAsync(
                    message.MessageId,
                    message.PopReceipt
                );
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task<SmtpConfiguration> GetSmtpCredentials()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            "https://notification-processor.azurewebsites.net/api/config/smtp"
        );
        return await response.Content
            .ReadFromJsonAsync<SmtpConfiguration>();
    }
}
```

### When to Use This Pattern
- ✅ **High volume** notification requests
- ✅ **Decoupling** between request submission and processing
- ✅ **Reliability** - messages persist until processed
- ✅ **Scalability** - multiple workers can consume queue
- ✅ **Rate limiting** - process at your own pace
- ✅ **Retry logic** - failed messages stay in queue

### Benefits
- **Asynchronous processing**: Don't block the caller
- **Load leveling**: Handle spikes in notification requests
- **Security**: Credentials never stored in queue
- **Fault tolerance**: Messages persist if Notification API is down

---

## Pattern 2: Queue for Credential Distribution (LESS SECURE)

**What goes in the queue**: SMTP/Twilio credentials
**Who puts it there**: NotificationProcessor
**Who consumes it**: Notification API
**Purpose**: Cache credentials to reduce HTTP calls

### Architecture
```
Notification API
    ↓ (HTTP: POST /api/config/queue)
NotificationProcessor (this service)
    ↓ (sends credentials to queue)
Azure Queue Storage
    ↓ (consumes once)
Notification API
    ↓ (caches credentials in memory/Redis for 5-10 min)
[Later when notification comes in]
    ↓ (uses cached credentials)
SMTP/Twilio
```

### How It Works

**Step 1**: Notification API requests credentials be sent to queue (once at startup or when cache expires)
```bash
POST /api/config/queue
{
  "notificationType": "email"
}
```

**Step 2**: NotificationProcessor sends credentials to queue
```json
{
  "requestId": "guid",
  "notificationType": "email",
  "configuration": {
    "host": "smtp.example.com",
    "port": 587,
    "username": "your-username",
    "password": "your-password",
    "fromEmail": "noreply@example.com",
    "fromName": "Notification System",
    "enableSsl": true
  },
  "success": true,
  "timestamp": "2026-01-17T12:00:00Z"
}
```

**Step 3**: Notification API consumes and caches
```csharp
public class CredentialCacheService : BackgroundService
{
    private readonly QueueClient _queueClient;
    private readonly IMemoryCache _cache;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _queueClient
                .ReceiveMessagesAsync(maxMessages: 10);

            foreach (var message in messages)
            {
                var response = JsonSerializer
                    .Deserialize<NotificationConfigResponse>(message.Body);

                // Cache credentials for 10 minutes
                _cache.Set(
                    $"credentials:{response.NotificationType}",
                    response.Configuration,
                    TimeSpan.FromMinutes(10)
                );

                await _queueClient.DeleteMessageAsync(
                    message.MessageId,
                    message.PopReceipt
                );
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

**Step 4**: Use cached credentials when sending
```csharp
public async Task SendEmail(EmailRequest request)
{
    // Try to get from cache
    if (!_cache.TryGetValue("credentials:email", out SmtpConfiguration smtpConfig))
    {
        // Cache miss - request credentials be sent to queue
        await RequestCredentialsToQueue("email");

        // Wait a bit and check cache again, or fallback to direct HTTP
        smtpConfig = await GetSmtpConfigDirectHTTP();
    }

    await SendEmailUsingSMTP(smtpConfig, request);
}
```

### When to Use This Pattern
- ✅ **Very high volume** (millions of emails/day)
- ✅ Want to **minimize HTTP calls** to NotificationProcessor
- ⚠️ Accept **security tradeoff** of credentials in queue

### Security Considerations
- ⚠️ **Credentials exposed in queue** (even if encrypted at rest)
- ⚠️ **Visibility timeout** could expose credentials
- ⚠️ **Queue access** must be tightly controlled
- ✅ Use **short cache expiration** (5-10 minutes)
- ✅ Use **Azure Queue encryption** at rest and in transit

---

## Pattern 3: Hybrid - Direct HTTP + Queue for Requests

**What goes in the queue**: Notification requests
**Credentials from**: Direct HTTP calls (with caching in Notification API)
**Best of both worlds**: Queue for requests, HTTP for credentials

### Architecture
```
External System
    ↓ (sends notification request)
Azure Queue Storage (notification requests)
    ↓ (consumes)
Notification API
    ├─ Check credential cache (in-memory/Redis)
    │   ├─ Cache hit → use cached credentials
    │   └─ Cache miss → HTTP call to NotificationProcessor
    ↓
SMTP/Twilio
```

### Implementation
```csharp
public class NotificationQueueProcessor : BackgroundService
{
    private readonly QueueClient _queueClient;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _queueClient.ReceiveMessagesAsync(10);

            foreach (var message in messages)
            {
                var notification = JsonSerializer
                    .Deserialize<NotificationRequest>(message.Body);

                // Get credentials (cached or HTTP)
                var smtpConfig = await GetSmtpCredentialsWithCache();

                // Process and send
                await ProcessNotification(notification, smtpConfig);

                // Remove from queue
                await _queueClient.DeleteMessageAsync(
                    message.MessageId,
                    message.PopReceipt
                );
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task<SmtpConfiguration> GetSmtpCredentialsWithCache()
    {
        // Try cache first
        if (_cache.TryGetValue("smtp_config", out SmtpConfiguration cached))
        {
            return cached;
        }

        // Cache miss - call NotificationProcessor via HTTP
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            "https://notification-processor.azurewebsites.net/api/config/smtp"
        );

        var config = await response.Content
            .ReadFromJsonAsync<SmtpConfiguration>();

        // Cache for 10 minutes
        _cache.Set("smtp_config", config, TimeSpan.FromMinutes(10));

        return config;
    }
}
```

### When to Use This Pattern
- ✅ **Recommended for most scenarios**
- ✅ Queue for asynchronous notification processing
- ✅ HTTP for secure credential retrieval
- ✅ Cache to reduce HTTP calls
- ✅ Best balance of security, performance, and reliability

---

## Pattern 4: Direct HTTP Only (No Queue)

**What goes in the queue**: Nothing
**Request flow**: Synchronous HTTP all the way

### Architecture
```
External System/User
    ↓ (HTTP: POST /api/notifications)
Notification API
    ↓ (HTTP: GET /api/config/smtp)
NotificationProcessor
    ↓ (returns credentials)
Notification API
    ↓ (processes template, sends)
SMTP/Twilio
    ↓ (responds)
Notification API
    ↓ (responds)
External System/User
```

### When to Use This Pattern
- ✅ **Low to medium volume** (< 1000/day)
- ✅ **Real-time notifications** needed immediately
- ✅ **Simple architecture** preferred
- ✅ No need for retry logic or queuing

---

## Comparison Table

| Pattern | Queue Contains | Credentials From | Security | Scalability | Complexity | Use Case |
|---------|---------------|------------------|----------|-------------|------------|----------|
| **#1: Queue for Requests** | Notification requests | HTTP (per request) | ✅ High | ✅✅✅ Excellent | ⭐⭐ Medium | High volume, async |
| **#2: Queue for Credentials** | SMTP/Twilio creds | Queue (cached) | ⚠️ Medium | ✅✅ Good | ⭐⭐⭐ High | Very high volume |
| **#3: Hybrid** | Notification requests | HTTP (cached) | ✅ High | ✅✅✅ Excellent | ⭐⭐ Medium | **RECOMMENDED** |
| **#4: Direct HTTP** | Nothing | HTTP (per request) | ✅ High | ✅ Basic | ⭐ Low | Low volume, simple |

---

## Recommended Setup for Your Use Case

Based on your sample request format and the queue URL provided, I recommend **Pattern #3: Hybrid**.

### Setup Steps

**1. External systems send notification requests to Azure Queue**:
```json
{
  "notificationId": "uuid",
  "type": "UserWelcome",
  "channels": ["email", "sms"],
  "recipient": { ... },
  "payload": { ... }
}
```

**2. Your Notification API**:
- Runs as a background service consuming from `https://clereviewst.queue.core.windows.net/notifications`
- When processing each message:
  - Checks cache for SMTP/Twilio credentials
  - If cache miss: Calls `GET /api/config/smtp` or `GET /api/config/twilio` from NotificationProcessor
  - Caches credentials for 10 minutes
  - Loads HTML template, merges with `payload` data
  - Sends email/SMS using cached credentials
  - Deletes message from queue on success

**3. NotificationProcessor (this service)**:
- Provides credentials via HTTP endpoints
- `POST /api/config/queue` endpoint exists but is optional (only use for Pattern #2)

### Why This Works Best

✅ **Security**: Credentials never stored in queue
✅ **Performance**: Credentials cached, only HTTP call every 10 min
✅ **Reliability**: Queue persists notification requests
✅ **Scalability**: Multiple Notification API instances can consume queue
✅ **Simplicity**: Clear separation of concerns

---

## Summary: What Goes Where

| Component | What It Does | What Goes In | What Comes Out |
|-----------|--------------|--------------|----------------|
| **Azure Queue** | Stores notification requests | `{notificationId, type, channels, recipient, payload}` | Consumed by Notification API |
| **NotificationProcessor** | Stores & exposes credentials | Configuration files | `{host, port, username, password, ...}` |
| **Notification API** | Processes & sends notifications | Queue messages + credentials from HTTP | Emails/SMS sent via SMTP/Twilio |

The queue is for **notification requests** (what to send), not credentials (how to send).

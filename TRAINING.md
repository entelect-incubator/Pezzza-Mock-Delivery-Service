# Training Materials: How the Mock Delivery Service Was Built

This document explains the architectural decisions, engineering patterns, and learning objectives behind the Pezzza Mock Delivery Service.

## 🎯 Learning Objectives

By studying this mock service, you'll learn:
1. **External API Integration** - How to call third-party services
2. **Async Callbacks (Webhooks)** - Long-running processes that notify you when complete
3. **Idempotency** - Handling duplicate requests safely
4. **State Machines** - Managing status transitions
5. **Error Handling** - Retries, timeouts, failures
6. **Background Jobs** - Processing work outside HTTP requests
7. **Clean Architecture** - Separation of concerns in .NET Minimal APIs
8. **Docker** - Containerizing and distributing applications

---

## 📐 Architecture Overview

### Why a Mock Service?

Real delivery APIs (like DoorDash, Uber Eats) are:
- **Expensive** - API credits cost money
- **Slow** - Real drivers take 30+ minutes
- **External** - You don't control them
- **Unreliable** - Subject to rate limits and outages

A mock service is:
- **Free** - No API costs
- **Fast** - Configurable delays (10 seconds instead of 30 minutes)
- **Owned** - You control the behavior
- **Teachable** - You can break it on purpose

### Design Decisions

#### 1. **In-Memory Storage** (No Database)

```csharp
private readonly ConcurrentDictionary<string, Delivery> _deliveries = new();
```

**Why?**
- Training focus: No complexity of databases
- Fast iteration: No migrations or setup
- Teaches concurrency: Must handle thread-safe operations
- Realistic for testing: Data loss on restart is OK for tests

**Enterprise Note:** Production would use PostgreSQL/MongoDB with persistence.

---

#### 2. **Background Worker** (State Transitions)

```csharp
public sealed class DeliverySimulationWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Poll every 5 seconds
            var pending = store.GetPendingDeliveries();
            foreach (var delivery in pending)
            {
                TransitionStatus(delivery);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
```

**Why This Approach?**

Real APIs use webhooks TO YOU, but someone still drives the delivery. In this mock:
- **Polling**: Service checks every 5 seconds
- **Status Transitions**: Automatically progresses deliveries
- **Webhook Callbacks**: Notifies your backend of changes

**Enterprise Pattern**: This is similar to message queues (RabbitMQ, Azure Service Bus) but simpler.

Status flow:
```
Created (0s) → PickedUp (30s) → OnTheWay (60s) → Delivered (90s)
```

**Development Settings** (`appsettings.Development.json`):
```json
{
  "Simulation": {
    "StatusTransitionDelaySeconds": 10,  // 10s for testing, not 30s
    "FailurePercentage": 20              // Higher failure rate for testing
  }
}
```

---

#### 3. **Idempotency** (Safe Duplicates)

```csharp
public bool TryAdd(Delivery delivery)
{
    if (!_deliveries.TryAdd(delivery.Id, delivery))
        return false;

    if (!string.IsNullOrEmpty(delivery.IdempotencyKey))
    {
        _idempotencyKeys.TryAdd(delivery.IdempotencyKey, delivery.Id);
    }
    return true;
}

// Endpoint checks:
var existing = store.GetByIdempotencyKey(request.IdempotencyKey);
if (existing != null)
{
    return Results.Ok(existing);  // Return same delivery
}
```

**Why This Matters:**

Network is unreliable. What if:
1. You POST `/deliveries` with order-123
2. Network timeout - but the delivery WAS created
3. Your code retries with same data
4. Without idempotency: TWO deliveries created
5. With idempotency: Same delivery returned

**Enterprise Use**: This is how banks transfer money safely. POST the same transfer 3 times = 1 transaction.

---

#### 4. **Webhook Service** (Async Notifications)

```csharp
public async Task SendWebhookAsync(Delivery delivery)
{
    if (delivery.Status == DeliveryStatus.PickedUp)
    {
        // HTTP POST to your callback URL with delivery status
        await client.PostAsJsonAsync(delivery.WebhookUrl, payload);
    }
}
```

**The Flow:**

```
┌─────────────────┐         ┌──────────────────┐
│  Mock Delivery  │         │  Your Pizza API  │
│    Service      │         │   (Pizza Store)  │
└─────────────────┘         └──────────────────┘
        │                            │
        │ POST /deliveries           │
        │◄───────────────────────────┤
        │ Return deliveryId          │
        │────────────────────────────►│
        │                            │
        │ (30 seconds pass)          │
        │ Status: PickedUp           │
        │                            │
        │ POST /webhooks/delivery    │
        │─────────────────────────────►
        │                            │
        │ (30 seconds pass)          │
        │ Status: OnTheWay           │
        │                            │
        │ POST /webhooks/delivery    │
        │─────────────────────────────►
```

**Why Webhooks?**

- **Pull** (Bad): Your API constantly asks "Status?" - wasteful
- **Push** (Good): Delivery service tells you when status changes - reactive

**Retry Logic:**

```csharp
while (retryCount <= maxRetries)
{
    try
    {
        var response = await client.PostAsJsonAsync(webhook, payload);
        if (response.IsSuccessStatusCode) return;
    }
    catch (Exception ex) { }
    
    retryCount++;
    // Exponential backoff: wait 2^retryCount seconds
    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
}
```

If your webhook endpoint is down:
- Attempt 1: Immediate
- Attempt 2: Wait 2 seconds
- Attempt 3: Wait 4 seconds
- Attempt 4: Wait 8 seconds
- If still failing: Logged and abandoned

---

#### 5. **Status Machine** (Enforced Transitions)

```csharp
private bool TransitionStatus(Delivery delivery) =>
    delivery.Status switch
    {
        DeliveryStatus.Created => TransitionToPickedUp(delivery),
        DeliveryStatus.PickedUp => TransitionToOnTheWay(delivery),
        DeliveryStatus.OnTheWay => TransitionToDelivered(delivery),
        _ => false
    };
```

**Why?**

Invalid transitions are prevented:
- Can't go from OnTheWay → Created (backwards in time!)
- Can't skip PickedUp (must follow sequence)
- Can't transition from terminal states (Delivered/Failed)

**Enterprise Pattern:** State machines are crucial in payment systems, order processing, etc.

---

#### 6. **Clean Code Structure**

```
src/
├── MockDelivery.Api/
│   ├── Program.cs           # Entry point, dependency injection
│   ├── Endpoints/
│   │   └── DeliveryEndpoints.cs    # HTTP routes
│   ├── Services/
│   │   ├── DeliveryStore.cs        # In-memory storage
│   │   └── WebhookService.cs       # Webhook delivery
│   └── Workers/
│       └── DeliverySimulationWorker.cs  # Background job
└── Common/
    ├── Models/
    │   ├── Delivery.cs             # Domain model
    │   └── DeliveryContracts.cs    # DTOs
    └── Settings.cs                 # Configuration
```

**Separation of Concerns:**
- **Endpoints**: HTTP request/response
- **Services**: Business logic
- **Models**: Domain entities
- **Workers**: Background jobs

Each has one responsibility.

---

## 🔧 Configuration & Flexibility

### Runtime Configuration

```bash
# Development (fast transitions)
dotnet run --environment Development

# Production (slow, realistic)
dotnet run --environment Production

# Docker (configurable)
docker run \
  -e MockDelivery__Simulation__StatusTransitionDelaySeconds=5 \
  -e MockDelivery__Simulation__FailurePercentage=25 \
  mock-delivery
```

### Scenarios You Can Test

#### 1. **Happy Path**
```json
{
  "orderId": "order-123",
  "webhookUrl": "https://your-api.com/webhooks"
}
```
Result: Created → PickedUp → OnTheWay → Delivered ✅

#### 2. **Forced Failure**
```json
{
  "orderId": "order-456",
  "forceFail": true
}
```
Result: Created → Failed immediately ❌

#### 3. **Random Failure**
```json
{
  "FailurePercentage": 30
}
```
Result: 30% of deliveries fail randomly (test error handling) 🎲

#### 4. **Slow API**
```json
{
  "EnableRandomDelays": true,
  "MaxRandomDelayMs": 2000
}
```
Result: Random delays up to 2 seconds (test timeouts) ⏱️

#### 5. **Idempotency**
```json
{
  "idempotencyKey": "same-key"
}
// POST twice with same key = one delivery
```

---

## 📝 Key Files & Why They Matter

### `DeliveryStore.cs`
Thread-safe in-memory storage. Uses `ConcurrentDictionary` because multiple threads might read/write simultaneously.

### `DeliverySimulationWorker.cs`
Background job that drives the simulation. Runs `ExecuteAsync` in a loop, transitioning deliveries.

### `WebhookService.cs`
Sends HTTP callbacks to your API. Includes retry logic for reliability.

### `DeliveryEndpoints.cs`
HTTP endpoints (REST API). Maps routes to handler functions.

### `Settings.cs`
Configuration classes. Map `appsettings.json` to strongly-typed C# objects.

---

## 🚀 Extension Points

### Add Email Notifications
```csharp
public interface IEmailService
{
    Task SendDeliveryUpdateAsync(string customerEmail, Delivery delivery);
}

// In worker:
await emailService.SendDeliveryUpdateAsync(customer.Email, delivery);
```

### Add SMS Updates
```csharp
public interface ISmsService
{
    Task SendSmsAsync(string phone, string message);
}

// In webhook service:
await smsService.SendSmsAsync(delivery.Phone, $"Your order is {delivery.Status}");
```

### Add Real Database Persistence
```csharp
// Replace ConcurrentDictionary with:
public class DeliveryRepository : IDeliveryStore
{
    private readonly IDbConnection _db;
    
    public async Task AddAsync(Delivery delivery)
    {
        await _db.ExecuteAsync(
            "INSERT INTO deliveries (id, orderId, status) VALUES (@id, @orderId, @status)",
            delivery);
    }
}
```

### Add GraphQL API
```csharp
app.MapGraphQL();

// Then query:
// {
//   delivery(id: "abc123") {
//     status
//     driver { name phone }
//     estimatedTime
//   }
// }
```

---

## 📊 How It Compares to Real Systems

| Aspect           | Mock Service            | Real Delivery API | Your Pizza Backend  |
| ---------------- | ----------------------- | ----------------- | ------------------- |
| **Data**         | In-memory               | Database          | Database            |
| **Latency**      | 0-500ms                 | 100-5000ms        | 50-500ms            |
| **Persistence**  | None (reset on restart) | Permanent         | Permanent           |
| **Webhooks**     | Simulated               | Real              | Must handle them    |
| **Costs**        | Free                    | Pay per request   | Internal            |
| **Availability** | Always 100%             | 99.9% SLA         | Your responsibility |

---

## 🧪 Testing Strategies

### Unit Tests (When Implemented)
```csharp
[Test]
public void TransitionPickedUpToOnTheWay_ShouldSucceed()
{
    var delivery = new Delivery { Status = DeliveryStatus.PickedUp };
    var result = TransitionStatus(delivery);
    
    Assert.That(result, Is.True);
    Assert.That(delivery.Status, Is.EqualTo(DeliveryStatus.OnTheWay));
}
```

### Integration Tests
```csharp
[Test]
public async Task CreateDelivery_ShouldReturn201()
{
    var response = await client.PostAsJsonAsync("/api/v1/deliveries", request);
    
    Assert.That(response.StatusCode, Is.EqualTo(201));
}
```

### Load Testing
```bash
# Simulate 100 concurrent users creating deliveries
k6 run load-test.js
```

---

## 🎓 Key Takeaways

1. **Mock services are production skills** - Every large company has them
2. **Background jobs** - Needed for long-running tasks outside HTTP
3. **Webhooks** - The smart way to handle async notifications
4. **Idempotency** - Makes your API robust to retries and network issues
5. **Configuration** - Same code, different behavior (dev vs prod)
6. **State machines** - Enforce valid transitions
7. **Owned infrastructure** - Beats relying on external SaaS

---

## 🔗 Enterprise Patterns Used

- ✅ **Repository Pattern** - `IDeliveryStore` abstraction
- ✅ **Dependency Injection** - Services in `Program.cs`
- ✅ **Configuration Management** - `appsettings.json` + `IOptions`
- ✅ **Logging** - Serilog structured logging
- ✅ **Background Jobs** - `BackgroundService` pattern
- ✅ **Webhooks** - Async push notifications
- ✅ **Idempotency** - Duplicate request handling
- ✅ **Health Checks** - `/health` endpoint
- ✅ **OpenAPI** - API documentation
- ✅ **Docker** - Containerization

---

## 📚 Further Learning

- [Microsoft Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [BackgroundService Pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [Webhooks in Practice](https://zapier.com/blog/webhook/)
- [Idempotent APIs](https://stripe.com/blog/idempotency)
- [State Machines](https://refactoring.guru/design-patterns/state)

---

**This is enterprise software architecture. You're learning the real deal.** 🚀

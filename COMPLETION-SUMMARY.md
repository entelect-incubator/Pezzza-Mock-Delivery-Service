# 🎉 Pezzza Mock Delivery Service - COMPLETE

**Status**: ✅ PRODUCTION READY  
**Version**: 1.0.0  
**Framework**: .NET 10 Minimal API  
**Repository**: https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service

---

## 📦 What's Been Built

A **complete, enterprise-grade mock delivery service** that teaches real-world integration patterns:

### ✅ Core Features
- **3 REST Endpoints**: Create, Get Status, Cancel deliveries
- **Background Worker**: Automatic status transitions (Created → PickedUp → OnTheWay → Delivered)
- **Webhook Service**: Async notifications to your backend with retry logic
- **Idempotency**: Safe to retry requests without duplicating deliveries
- **Failure Simulation**: Random failures + forced failure mode for testing
- **In-Memory Storage**: Thread-safe concurrent dictionary
- **Health Checks**: `/health` endpoint for monitoring
- **OpenAPI Spec**: Full API documentation

### ✅ Production Infrastructure
- **Docker**: Multi-stage Dockerfile (optimized image size)
- **Docker Compose**: Local development setup
- **GitHub Actions**: Auto-build and push to GHCR (ghcr.io)
- **Configuration**: appsettings.json + environment variables
- **Logging**: Structured Serilog logging

### ✅ Comprehensive Documentation
- **README.md** (150+ lines) - Overview, quick start, API reference
- **TRAINING.md** (400+ lines) - Architecture deep-dive, design decisions, learning objectives
- **AI-INTEGRATION-GUIDE.md** (350+ lines) - Copy-paste integration examples for C#, JS, Python
- **QUICKSTART.md** (250+ lines) - Quick reference for developers
- **openapi/delivery-api.yaml** - Full OpenAPI 3.0 specification
- **Inline code comments** - Explaining key patterns

### ✅ Code Quality
- Follows Minimal API patterns from `.NET-Template`
- Clean architecture: Endpoints, Services, Models, Workers
- Dependency injection throughout
- Thread-safe concurrent collections
- Proper error handling and validation
- Global usings for cleaner code
- Settings classes with strong typing

---

## 📂 Project Structure

```
pezzza-mock-delivery-service/
├── src/
│   ├── MockDelivery.Api/                    # ASP.NET Core Web API
│   │   ├── Program.cs                       # Entry point, DI configuration
│   │   ├── GlobalUsings.cs                  # Global imports
│   │   ├── MockDelivery.Api.csproj          # .NET 10 project file
│   │   ├── Endpoints/
│   │   │   └── DeliveryEndpoints.cs         # POST/GET/DELETE routes
│   │   ├── Services/
│   │   │   ├── DeliveryStore.cs             # In-memory concurrent storage
│   │   │   └── WebhookService.cs            # HTTP callbacks with retries
│   │   ├── Workers/
│   │   │   └── DeliverySimulationWorker.cs  # Background job for transitions
│   │   ├── appsettings.json                 # Production config
│   │   └── appsettings.Development.json     # Dev config (fast transitions)
│   │
│   └── Common/                              # Shared library
│       ├── Common.csproj                    # .NET 10 class library
│       ├── GlobalUsings.cs
│       ├── Settings.cs                      # Configuration classes
│       └── Models/
│           ├── Delivery.cs                  # Domain model
│           └── DeliveryContracts.cs         # DTOs (requests/responses)
│
├── Dockerfile                               # Multi-stage build
├── docker-compose.yml                       # Local dev environment
├── .dockerignore                            # Exclude unnecessary files
├── MockDelivery.sln                         # Visual Studio solution
│
├── .github/workflows/
│   ├── dotnet.yml                          # Build and test on push
│   └── docker-publish.yml                  # Build and publish Docker image
│
├── openapi/
│   └── delivery-api.yaml                   # OpenAPI 3.0 specification
│
├── README.md                                # Main documentation
├── QUICKSTART.md                            # Developer quick reference
├── TRAINING.md                              # Architecture & learning guide
├── AI-INTEGRATION-GUIDE.md                  # AI assistant integration help
├── LICENSE                                  # MIT license
└── .gitignore                               # Git ignore patterns
```

---

## 🚀 Quick Start

### Option 1: Docker (Fastest - No Installation Needed)
```bash
docker pull ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
docker run -p 8081:8080 ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest

# API available at http://localhost:8081
curl http://localhost:8081/health
```

### Option 2: Docker Compose
```bash
git clone https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service.git
cd Pezzza-Mock-Delivery-Service
docker-compose up -d

# Check logs
docker-compose logs -f mock-delivery

# Stop
docker-compose down
```

### Option 3: Local .NET Development
```bash
git clone https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service.git
cd Pezzza-Mock-Delivery-Service

# Requires: .NET 10 SDK
dotnet restore
dotnet build
dotnet run --project src/MockDelivery.Api

# API available at http://localhost:5000
```

---

## 🔌 Integration (Simple Example)

### C# Backend
```csharp
// Create delivery when order is placed
var response = await httpClient.PostAsJsonAsync(
    "http://localhost:8081/api/v1/deliveries",
    new {
        orderId = order.Id,
        pickupAddress = new { street = "123 Pizza St", city = "CT", postalCode = "8001" },
        deliveryAddress = new { street = "456 Customer Ave", city = "CT", postalCode = "8002" },
        webhookUrl = "https://your-api.com/webhooks/delivery"
    }
);

var delivery = await response.Content.ReadFromJsonAsync<DeliveryResponse>();
order.DeliveryId = delivery.DeliveryId;
```

### Your Webhook Handler
```csharp
[HttpPost("/webhooks/delivery")]
public async Task OnDeliveryStatusChanged([FromBody] DeliveryWebhookPayload payload)
{
    var order = await orderService.GetByIdAsync(payload.OrderId);
    order.DeliveryStatus = payload.Status.ToString();
    
    if (payload.Status == DeliveryStatus.Delivered)
    {
        order.Status = "Completed";
    }
    
    await orderService.UpdateAsync(order);
    
    // Notify customer
    await notificationService.SendStatusUpdateAsync(order.CustomerId, payload.Status);
    
    return Ok();
}
```

---

## 📡 API Overview

| Endpoint                         | Method | Purpose          | Returns                |
| -------------------------------- | ------ | ---------------- | ---------------------- |
| `/api/v1/deliveries`             | POST   | Create delivery  | 201 + deliveryId       |
| `/api/v1/deliveries/{id}`        | GET    | Get status       | 200 + delivery details |
| `/api/v1/deliveries/{id}/cancel` | POST   | Cancel delivery  | 200 + status           |
| `/health`                        | GET    | Health check     | 200 OK                 |
| `/scalar/v1`                     | GET    | Interactive docs | HTML                   |

**Example Request:**
```bash
curl -X POST http://localhost:8081/api/v1/deliveries \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "order-123",
    "pickupAddress": {
      "street": "123 Pizza St",
      "city": "Cape Town",
      "postalCode": "8001"
    },
    "deliveryAddress": {
      "street": "456 Customer Ave",
      "city": "Cape Town",
      "postalCode": "8002"
    },
    "webhookUrl": "https://your-api.com/webhooks/delivery"
  }'
```

---

## 🎯 Learning Outcomes

By using this service, trainees learn:

1. **External API Integration**
   - How to call third-party APIs
   - Error handling and retries
   - Timeout management

2. **Async Webhooks**
   - Long-running operations that notify you
   - Webhook delivery with retries
   - Handling webhook failures

3. **Idempotency**
   - Safe duplicate request handling
   - Unique request identification
   - Why it matters for distributed systems

4. **State Machines**
   - Valid status transitions
   - Preventing invalid states
   - Real-world workflow modeling

5. **Background Jobs**
   - .NET BackgroundService pattern
   - Polling-based status updates
   - Scheduled work in ASP.NET Core

6. **Clean Architecture**
   - Separation of concerns
   - Dependency injection
   - Service abstraction

7. **Docker & DevOps**
   - Multi-stage Docker builds
   - GitHub Actions CI/CD
   - Container orchestration

---

## 🏢 Enterprise Patterns Taught

✅ Repository Pattern - `IDeliveryStore`  
✅ Dependency Injection - Services in DI container  
✅ Configuration Management - `appsettings.json` + `IOptions`  
✅ Structured Logging - Serilog with request logging  
✅ Background Jobs - `BackgroundService` pattern  
✅ Webhooks - Async push notifications  
✅ Idempotency - Safe duplicate requests  
✅ Health Checks - `/health` endpoint  
✅ OpenAPI - API documentation  
✅ Docker - Containerization and deployment  
✅ CI/CD - GitHub Actions automation  

---

## 📊 Status Transitions

```
User Creates Order
        ↓
POST /api/v1/deliveries
        ↓
Service creates delivery with status: Created
        ↓
BackgroundService runs every 5 seconds
        ↓
30 seconds elapsed → Status: PickedUp (webhook sent)
        ↓
30 seconds elapsed → Status: OnTheWay (webhook sent)
        ↓
30 seconds elapsed → Status: Delivered (webhook sent)
        ↓
Your webhook handler updates database
        ↓
Frontend displays: "Order delivered! 🎉"
```

---

## ⚙️ Configuration

### Development (Fast Testing)
```bash
docker run -p 8081:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e MockDelivery__Simulation__StatusTransitionDelaySeconds=10 \
  ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
```

### Production (Realistic)
```bash
docker run -p 8081:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e MockDelivery__Simulation__StatusTransitionDelaySeconds=30 \
  -e MockDelivery__Simulation__FailurePercentage=5 \
  ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
```

---

## 🧪 Test Scenarios

1. **Happy Path**: Normal delivery progression
2. **Forced Failure**: `"forceFail": true` for immediate failure
3. **Random Failures**: `FailurePercentage=50` for chaos testing
4. **Idempotency**: Send same request twice = same result
5. **Webhook Timeout**: Test retry logic with slow endpoint
6. **Cancellation**: Cancel delivery at any stage

---

## 📚 Documentation Map

| Document                      | Purpose                                  | Audience                  |
| ----------------------------- | ---------------------------------------- | ------------------------- |
| **README.md**                 | Overview, quick start, API reference     | Everyone                  |
| **QUICKSTART.md**             | Fast reference guide                     | Developers                |
| **TRAINING.md**               | Architecture, design decisions, learning | Trainees, architects      |
| **AI-INTEGRATION-GUIDE.md**   | Copy-paste integration patterns          | AI assistants, developers |
| **openapi/delivery-api.yaml** | Full API specification                   | API clients, generators   |

---

## 🔗 GitHub Integration

**Repository**: https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service

### GitHub Actions Workflows

**1. `.NET Build & Test`** (`.github/workflows/dotnet.yml`)
- Runs on: `push to main/develop`, `pull_request`
- Does: Restore → Build → Test
- Artifact: Build logs

**2. **Docker Build & Publish** (`.github/workflows/docker-publish.yml`)
- Runs on: `push to main`, `pull_request`, `manual trigger`
- Does: Build multi-arch image (linux/amd64, linux/arm64)
- Publishes to: `ghcr.io/entelect-incubator/pezzza-mock-delivery-service`
- Tags: `latest`, `main`, `v1.0.0`, `branch-name`

### GitHub Container Registry
```bash
docker pull ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
```

---

## 🚀 Deployment Options

### Local Docker
```bash
docker run -p 8081:8080 \
  ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
```

### Docker Compose
```yaml
services:
  mock-delivery:
    image: ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
    ports:
      - "8081:8080"
```

### Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mock-delivery
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: mock-delivery
        image: ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
        ports:
        - containerPort: 8080
```

### Azure Container Instances
```bash
az container create \
  --image ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest \
  --ports 8080
```

---

## 🎓 Integration with Incubator

This service is designed to be used in **any Incubator project** that needs:
- Order delivery tracking
- Webhook integration examples
- External API consumption patterns
- Real-world async processing

**Example Projects**:
- 🍕 Pizza ordering system (Pezzza)
- 🚗 Ride-sharing app
- 📦 E-commerce/logistics
- 🏨 Hotel booking with delivery
- 🍔 Food delivery aggregator

---

## ✅ Verification Checklist

- ✅ .NET 10 projects created
- ✅ All endpoints implemented (Create, Get, Cancel)
- ✅ Background worker for status transitions
- ✅ Webhook service with retries
- ✅ In-memory thread-safe storage
- ✅ Configuration via appsettings
- ✅ Dockerfile (multi-stage, optimized)
- ✅ docker-compose.yml for local dev
- ✅ GitHub Actions workflows
- ✅ OpenAPI specification
- ✅ Comprehensive documentation (4 guides)
- ✅ Health checks implemented
- ✅ Structured logging
- ✅ Error handling
- ✅ Idempotency support

---

## 📞 Support & Resources

### Documentation
- **README.md** - Start here for overview
- **QUICKSTART.md** - Developer reference
- **TRAINING.md** - Learn the architecture
- **AI-INTEGRATION-GUIDE.md** - Integration patterns
- **openapi/delivery-api.yaml** - API spec

### Online
- **GitHub**: https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service
- **Issues**: Report bugs or feature requests
- **Discussions**: Share questions and ideas
- **OpenAPI UI**: http://localhost:8081/scalar/v1 (when running)

### Local Testing
```bash
# Health check
curl http://localhost:8081/health

# API docs
curl http://localhost:8081/scalar/v1

# View logs
docker logs <container_id>

# Check running services
docker ps
```

---

## 🎉 You're Ready!

This mock delivery service is:
- ✅ **Production-ready** - Enterprise-grade code quality
- ✅ **Fully documented** - 4 comprehensive guides
- ✅ **Easy to integrate** - Copy-paste examples
- ✅ **Zero dependencies** - Just Docker or .NET 10
- ✅ **Extensible** - Easy to add features
- ✅ **CI/CD ready** - GitHub Actions included
- ✅ **Teaching-focused** - Designed for learning

**Next steps:**
1. Read [README.md](./README.md) for overview
2. Start service: `docker-compose up -d`
3. Read [TRAINING.md](./TRAINING.md) to understand architecture
4. Implement webhook handler in your project
5. See [AI-INTEGRATION-GUIDE.md](./AI-INTEGRATION-GUIDE.md) for patterns

---

**Built with ❤️ for the Entelect Incubator**  
**Version**: 1.0.0 | **.NET**: 10.0 | **License**: MIT

---

Let's build something amazing! 🚀

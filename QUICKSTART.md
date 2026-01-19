# Quick Reference - Pezzza Mock Delivery Service

## 🚀 Start Service

```bash
# Docker (easiest)
docker run -p 8081:8080 ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest

# Docker Compose
docker-compose up -d

# Local .NET
dotnet run --project src/MockDelivery.Api
```

## 📡 API Endpoints

| Method | Endpoint                         | Purpose           |
| ------ | -------------------------------- | ----------------- |
| `POST` | `/api/v1/deliveries`             | Create delivery   |
| `GET`  | `/api/v1/deliveries/{id}`        | Get status        |
| `POST` | `/api/v1/deliveries/{id}/cancel` | Cancel delivery   |
| `GET`  | `/health`                        | Health check      |
| `GET`  | `/scalar/v1`                     | API documentation |

## 🔄 Delivery Status Flow

```
Created (0s)
    ↓
PickedUp (30s)
    ↓
OnTheWay (60s)
    ↓
Delivered (90s) ✅

OR at any point:
    ↓
Failed ❌ or Cancelled 🚫
```

## 📝 Create Delivery Example

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
    "idempotencyKey": "unique-key-123",
    "webhookUrl": "https://your-api.com/webhooks/delivery",
    "forceFail": false
  }'
```

**Response:**
```json
{
  "deliveryId": "abc123def456",
  "status": "Created",
  "createdAt": "2026-01-18T14:30:00Z",
  "message": "Delivery created successfully"
}
```

## 🔍 Check Status

```bash
curl http://localhost:8081/api/v1/deliveries/abc123def456
```

**Response:**
```json
{
  "deliveryId": "abc123def456",
  "orderId": "order-123",
  "status": "OnTheWay",
  "createdAt": "2026-01-18T14:30:00Z",
  "pickedUpAt": "2026-01-18T14:30:30Z",
  "deliveredAt": null,
  "driver": {
    "name": "John Smith",
    "phone": "+27 72 123 4567",
    "vehicleNumber": "CA 12 AB"
  }
}
```

## ❌ Cancel Delivery

```bash
curl -X POST http://localhost:8081/api/v1/deliveries/abc123def456/cancel \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer request"}'
```

## 🔔 Webhook Payload

When status changes, service POSTs to your `webhookUrl`:

```json
{
  "deliveryId": "abc123def456",
  "orderId": "order-123",
  "status": "PickedUp",
  "timestamp": "2026-01-18T14:30:30Z",
  "failureReason": null,
  "driver": {
    "name": "John Smith",
    "phone": "+27 72 123 4567",
    "vehicleNumber": "CA 12 AB"
  }
}
```

## ⚙️ Configuration

### Environment Variables
```bash
# Status transition delay (seconds)
MockDelivery__Simulation__StatusTransitionDelaySeconds=30

# Chance of random failure (0-100)
MockDelivery__Simulation__FailurePercentage=10

# Enable random API delays
MockDelivery__Simulation__EnableRandomDelays=true

# Max random delay (milliseconds)
MockDelivery__Simulation__MaxRandomDelayMs=500

# Enable webhook callbacks
MockDelivery__Webhook__Enabled=true

# Webhook retry attempts
MockDelivery__Webhook__RetryCount=3

# Webhook timeout (seconds)
MockDelivery__Webhook__TimeoutSeconds=30
```

### Development Settings
```bash
docker run -p 8081:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e MockDelivery__Simulation__StatusTransitionDelaySeconds=10 \
  -e MockDelivery__Simulation__FailurePercentage=20 \
  ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
```

## 🧪 Test Scenarios

### 1. Normal Delivery
```bash
POST /deliveries with normal payload
→ Created → PickedUp → OnTheWay → Delivered
```

### 2. Forced Failure
```bash
POST /deliveries with "forceFail": true
→ Created → Failed immediately
```

### 3. Random Failure
```bash
Set FailurePercentage=50
→ 50% chance delivery fails at OnTheWay stage
```

### 4. Idempotent Requests
```bash
POST /deliveries with "idempotencyKey": "same-key"
POST /deliveries with "idempotencyKey": "same-key"
→ Both return same deliveryId (safe duplicate)
```

### 5. No Webhook
```bash
POST /deliveries without webhookUrl
→ Delivery progresses normally, no callbacks
```

## 🏗️ Project Structure

```
src/
├── MockDelivery.Api/              # ASP.NET Core project
│   ├── Program.cs                 # Entry point, DI setup
│   ├── Endpoints/
│   │   └── DeliveryEndpoints.cs   # HTTP routes
│   ├── Services/
│   │   ├── DeliveryStore.cs       # In-memory storage
│   │   └── WebhookService.cs      # Webhook delivery
│   ├── Workers/
│   │   └── DeliverySimulationWorker.cs  # Status transitions
│   ├── appsettings.json           # Configuration
│   └── appsettings.Development.json
└── Common/                         # Shared library
    ├── Models/
    │   ├── Delivery.cs            # Domain model
    │   └── DeliveryContracts.cs   # DTOs
    └── Settings.cs                # Config classes
```

## 🛠️ Development Commands

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run locally
dotnet run --project src/MockDelivery.Api

# Run in watch mode
dotnet watch run --project src/MockDelivery.Api

# Build Docker image
docker build -t mock-delivery:local .

# Run with docker-compose
docker-compose up -d
docker-compose logs -f
docker-compose down
```

## 📊 Integration Checklist

- [ ] Service running on port 8081
- [ ] Can create delivery: `curl -X POST http://localhost:8081/api/v1/deliveries`
- [ ] Can get status: `curl http://localhost:8081/api/v1/deliveries/{id}`
- [ ] Webhook endpoint created in your API
- [ ] Database updated with DeliveryId field
- [ ] Webhook handler implemented
- [ ] Frontend displays delivery status
- [ ] Error handling for failed webhooks
- [ ] Idempotency implemented
- [ ] Tests passing

## 📚 Documentation

- **README.md** - Overview and quick start
- **TRAINING.md** - Architecture deep-dive and learning objectives
- **AI-INTEGRATION-GUIDE.md** - Integration patterns for AI assistants
- **openapi/delivery-api.yaml** - Full API specification
- **http://localhost:8081/scalar/v1** - Interactive API docs (when running)

## 🔗 GitHub

- **Repository**: https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service
- **Issues**: Report bugs and feature requests
- **Releases**: https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service/releases
- **Container Registry**: ghcr.io/entelect-incubator/pezzza-mock-delivery-service

## 🐛 Debugging

```bash
# Health check
curl http://localhost:8081/health

# View logs
docker logs -f <container_id>

# Check service is responding
curl -v http://localhost:8081/api/v1/deliveries/{id}

# Verify webhook endpoint accessibility
curl -X POST http://your-api.com/webhooks/delivery -d '{"test":true}'

# Scale to 2 instances (Kubernetes)
kubectl scale deployment mock-delivery --replicas=2
```

## 🚀 Deployment

### Docker
```bash
docker pull ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
docker run -p 8081:8080 ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
```

### Kubernetes
```bash
kubectl create deployment mock-delivery \
  --image=ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
kubectl expose deployment mock-delivery --port=8080
```

### Azure Container Instances
```bash
az container create \
  --resource-group my-group \
  --name mock-delivery \
  --image ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest \
  --ports 8080 \
  --environment-variables \
    MockDelivery__Simulation__StatusTransitionDelaySeconds=30
```

## 💡 Tips

- **Test idempotency**: Send same POST twice, verify same deliveryId returned
- **Monitor webhooks**: Enable logging to see webhook delivery status
- **Simulate failures**: Use `forceFail: true` to test error handling
- **Fast testing**: Set `StatusTransitionDelaySeconds=5` for development
- **No persistence**: Data is lost on restart (by design for training)

## 📞 Getting Help

1. Check [TRAINING.md](./TRAINING.md) for architecture explanation
2. Review [AI-INTEGRATION-GUIDE.md](./AI-INTEGRATION-GUIDE.md) for integration patterns
3. Visit http://localhost:8081/scalar/v1 for API documentation
4. Check [README.md](./README.md) for troubleshooting
5. Open issue on GitHub with full error logs

---

**Version**: 1.0.0 | **Built with**: .NET 10 | **License**: MIT

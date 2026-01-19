# Pezzza Mock Delivery Service

A production-quality mock delivery API for training, testing, and integration purposes. Simulates a real-world third-party delivery provider with webhooks, status transitions, failures, retries, and idempotency.

[![Build](https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service/actions/workflows/dotnet.yml/badge.svg)](https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service/actions)
[![Docker](https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service/actions)

## 🎯 Purpose

This service teaches enterprise integration patterns:
- External API integration
- Async webhook callbacks  
- Failure handling and retries
- Idempotency
- Long-running process simulation
- State machine design

**Perfect for**: Training environments, integration testing, demos, and CI/CD pipelines.

---

## 🚀 Quick Start

### Option 1: Docker (Fastest)

```bash
# Pull from GitHub Container Registry
docker pull ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest

# Run
docker run -p 8081:8080 ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest

# Test
curl http://localhost:8081/health
```

### Option 2: Docker Compose

```bash
# Clone repository
git clone https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service.git
cd Pezzza-Mock-Delivery-Service

# Start service
docker-compose up -d

# View logs
docker-compose logs -f

# Stop service
docker-compose down
```

### Option 3: Build from Source

```bash
# Prerequisites: .NET 10 SDK

# Restore and build
dotnet restore
dotnet build

# Run
cd src/MockDelivery.Api
dotnet run

# API available at http://localhost:5000
```

---

## 📖 API Reference

See full documentation in the repository or visit the OpenAPI documentation at `http://localhost:8081/scalar/v1` when running.

### Key Endpoints

- `POST /api/v1/deliveries` - Create new delivery
- `GET /api/v1/deliveries/{id}` - Get delivery status
- `POST /api/v1/deliveries/{id}/cancel` - Cancel delivery

### Example: Create Delivery

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

## 🔔 Features

✅ **Realistic Status Transitions**: Created → PickedUp → OnTheWay → Delivered  
✅ **Webhook Callbacks**: Automatic notifications on status changes  
✅ **Idempotency**: Duplicate requests return same result  
✅ **Failure Simulation**: Random failures, forced failures, configurable rates  
✅ **Random Delays**: Simulates real API latency  
✅ **Retry Logic**: Automatic webhook retries with exponential backoff  
✅ **Health Checks**: `/health` endpoint for monitoring  
✅ **OpenAPI Spec**: Interactive API documentation  

---

## 📚 Documentation

- [TRAINING.md](./TRAINING.md) - How this mock was built and architectural decisions
- [AI-INTEGRATION-GUIDE.md](./AI-INTEGRATION-GUIDE.md) - Integration guide for AI assistants
- [OpenAPI Spec](./openapi/delivery-api.yaml) - Full API contract
- [Docker Hub](https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service/pkgs/container/pezzza-mock-delivery-service) - Container images

---

## ⚙️ Configuration

Environment variables:

```bash
MockDelivery__Simulation__StatusTransitionDelaySeconds=30
MockDelivery__Simulation__FailurePercentage=10
MockDelivery__Simulation__EnableRandomDelays=true
MockDelivery__Webhook__Enabled=true
MockDelivery__Webhook__RetryCount=3
```

See `appsettings.json` for all options.

---

## 🛠️ Development

```bash
# Run in watch mode
cd src/MockDelivery.Api
dotnet watch run

# Build Docker image
docker build -t mock-delivery:local .

# Run with docker-compose
docker-compose up
```

---

## 📄 License

MIT - see [LICENSE](./LICENSE)

---

**Built with**: .NET 9 | Minimal APIs | Serilog | Docker
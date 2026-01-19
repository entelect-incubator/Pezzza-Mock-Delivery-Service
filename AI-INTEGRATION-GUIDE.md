# AI Integration Guide - Mock Delivery Service

For AI assistants integrating this service into other Incubator projects.

---

## 🎯 What This Service Provides

A **reusable, production-quality mock delivery API** that:
- ✅ Simulates realistic delivery workflows
- ✅ Provides webhook callbacks for async integration
- ✅ Supports idempotent requests
- ✅ Handles failures and retries
- ✅ Requires **zero configuration** to run
- ✅ Ships as a Docker image

**Perfect for**: E-commerce, food delivery, logistics, or any order fulfillment system.

---

## 🚀 Quick Integration (Copy-Paste)

### Step 1: Start the Service

```bash
# Option A: Pull from registry (fastest)
docker pull ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
docker run -p 8081:8080 ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest

# Option B: Clone and run locally
git clone https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service.git
cd Pezzza-Mock-Delivery-Service
docker-compose up -d
```

Service now available at: `http://localhost:8081/api/v1`

### Step 2: Integrate Into Your Backend

**C# / .NET**
```csharp
public class OrderService
{
    private readonly HttpClient _httpClient;
    
    public async Task<string> CreateDeliveryAsync(Order order)
    {
        var request = new
        {
            orderId = order.Id,
            pickupAddress = new
            {
                street = "123 Pizza St",
                city = "Cape Town",
                postalCode = "8001"
            },
            deliveryAddress = new
            {
                street = order.DeliveryAddress.Street,
                city = order.DeliveryAddress.City,
                postalCode = order.DeliveryAddress.PostalCode
            },
            idempotencyKey = order.Id,  // Ensures safety on retries
            webhookUrl = "https://your-api.com/webhooks/delivery"
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8081/api/v1/deliveries",
            request);
        
        var delivery = await response.Content.ReadFromJsonAsync<DeliveryResponse>();
        return delivery.DeliveryId;
    }
    
    public async Task<DeliveryResponse> GetDeliveryStatusAsync(string deliveryId)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:8081/api/v1/deliveries/{deliveryId}");
        
        return await response.Content.ReadFromJsonAsync<DeliveryResponse>();
    }
}
```

**JavaScript / Node.js**
```javascript
class DeliveryService {
  async createDelivery(order) {
    const response = await fetch('http://localhost:8081/api/v1/deliveries', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        orderId: order.id,
        pickupAddress: {
          street: '123 Pizza St',
          city: 'Cape Town',
          postalCode: '8001'
        },
        deliveryAddress: {
          street: order.deliveryAddress.street,
          city: order.deliveryAddress.city,
          postalCode: order.deliveryAddress.postalCode
        },
        idempotencyKey: order.id,
        webhookUrl: 'https://your-api.com/webhooks/delivery'
      })
    });
    
    const delivery = await response.json();
    return delivery.deliveryId;
  }

  async getDeliveryStatus(deliveryId) {
    const response = await fetch(
      `http://localhost:8081/api/v1/deliveries/${deliveryId}`
    );
    return await response.json();
  }
}
```

**Python**
```python
import requests

class DeliveryService:
    def __init__(self, base_url='http://localhost:8081/api/v1'):
        self.base_url = base_url
    
    def create_delivery(self, order):
        payload = {
            'orderId': order['id'],
            'pickupAddress': {
                'street': '123 Pizza St',
                'city': 'Cape Town',
                'postalCode': '8001'
            },
            'deliveryAddress': {
                'street': order['delivery_address']['street'],
                'city': order['delivery_address']['city'],
                'postalCode': order['delivery_address']['postal_code']
            },
            'idempotencyKey': order['id'],
            'webhookUrl': 'https://your-api.com/webhooks/delivery'
        }
        
        response = requests.post(
            f'{self.base_url}/deliveries',
            json=payload
        )
        delivery = response.json()
        return delivery['deliveryId']
    
    def get_delivery_status(self, delivery_id):
        response = requests.get(
            f'{self.base_url}/deliveries/{delivery_id}'
        )
        return response.json()
```

### Step 3: Add Webhook Handler

Your backend must expose a webhook endpoint to receive delivery status updates:

**C# / .NET**
```csharp
[HttpPost("/webhooks/delivery")]
public async Task<IActionResult> OnDeliveryStatusChanged(
    [FromBody] DeliveryWebhookPayload payload,
    [FromServices] IOrderRepository orders)
{
    var order = await orders.GetByIdAsync(payload.OrderId);
    
    // Update order status based on delivery status
    order.DeliveryStatus = payload.Status.ToString();
    order.Driver = payload.Driver;
    
    if (payload.Status == DeliveryStatus.Delivered)
    {
        order.Status = OrderStatus.Completed;
    }
    else if (payload.Status == DeliveryStatus.Failed)
    {
        order.Status = OrderStatus.DeliveryFailed;
    }
    
    await orders.UpdateAsync(order);
    return Ok();
}
```

**JavaScript / Express.js**
```javascript
app.post('/webhooks/delivery', async (req, res) => {
  const { deliveryId, orderId, status, driver, failureReason } = req.body;
  
  // Update database
  const order = await Order.findById(orderId);
  order.deliveryStatus = status;
  order.driver = driver;
  
  if (status === 'Delivered') {
    order.status = 'completed';
  } else if (status === 'Failed') {
    order.status = 'delivery_failed';
    order.failureReason = failureReason;
  }
  
  await order.save();
  
  // Notify customer (send email/SMS)
  await notifyCustomer(order, status);
  
  res.ok();
});
```

---

## 📋 API Reference (Quick)

### Create Delivery
```
POST /api/v1/deliveries

{
  "orderId": "order-123",
  "pickupAddress": { "street": "...", "city": "...", "postalCode": "..." },
  "deliveryAddress": { "street": "...", "city": "...", "postalCode": "..." },
  "idempotencyKey": "unique-key",  // Optional but recommended
  "webhookUrl": "https://your-api.com/webhooks/delivery",  // Where to send updates
  "forceFail": false  // For testing: forces immediate failure
}

Response: 201 Created
{
  "deliveryId": "abc123def456",
  "status": "Created",
  "createdAt": "2026-01-18T14:30:00Z"
}
```

### Get Status
```
GET /api/v1/deliveries/{id}

Response: 200 OK
{
  "deliveryId": "abc123def456",
  "orderId": "order-123",
  "status": "OnTheWay",  // Created, PickedUp, OnTheWay, Delivered, Failed, Cancelled
  "createdAt": "...",
  "pickedUpAt": "...",
  "deliveredAt": null,
  "driver": { "name": "John", "phone": "+27...", "vehicleNumber": "..." }
}
```

### Cancel Delivery
```
POST /api/v1/deliveries/{id}/cancel

{
  "reason": "Customer cancelled order"
}

Response: 200 OK
```

---

## ⚙️ Configuration for Your Environment

### Local Development
```bash
docker run -p 8081:8080 \
  -e MockDelivery__Simulation__StatusTransitionDelaySeconds=10 \
  -e MockDelivery__Simulation__FailurePercentage=20 \
  ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
```

### Docker Compose
```yaml
services:
  pizza-api:
    build: ./PizzaBackend
    ports:
      - "5000:5000"
    environment:
      - DeliveryServiceUrl=http://mock-delivery:8080

  mock-delivery:
    image: ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
    ports:
      - "8081:8080"
    environment:
      - MockDelivery__Simulation__StatusTransitionDelaySeconds=10
      - MockDelivery__Simulation__FailurePercentage=15
```

### Kubernetes
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: mock-delivery-config
data:
  StatusTransitionDelaySeconds: "30"
  FailurePercentage: "10"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mock-delivery
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mock-delivery
  template:
    metadata:
      labels:
        app: mock-delivery
    spec:
      containers:
      - name: mock-delivery
        image: ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest
        ports:
        - containerPort: 8080
        env:
        - name: MockDelivery__Simulation__StatusTransitionDelaySeconds
          valueFrom:
            configMapKeyRef:
              name: mock-delivery-config
              key: StatusTransitionDelaySeconds
```

---

## 🧪 Testing Scenarios

### Test 1: Happy Path
```bash
curl -X POST http://localhost:8081/api/v1/deliveries \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "test-1",
    "pickupAddress": {"street":"123 Pizza St","city":"CT","postalCode":"8001"},
    "deliveryAddress": {"street":"456 Customer Ave","city":"CT","postalCode":"8002"},
    "webhookUrl": "http://your-api.com/webhooks/delivery"
  }'

# Wait 30+ seconds, check GET endpoint
curl http://localhost:8081/api/v1/deliveries/{deliveryId}
# Status progresses: Created → PickedUp → OnTheWay → Delivered
```

### Test 2: Forced Failure
```bash
curl -X POST http://localhost:8081/api/v1/deliveries \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "test-fail",
    "pickupAddress": {...},
    "deliveryAddress": {...},
    "forceFail": true  # ← Forces immediate failure
  }'

# Status will be: Created → Failed
```

### Test 3: Idempotency
```bash
# Request 1
curl -X POST http://localhost:8081/api/v1/deliveries \
  -d '{"orderId":"test","idempotencyKey":"unique-123",...}'
# Response: {"deliveryId":"abc123",...}

# Request 2 (identical)
curl -X POST http://localhost:8081/api/v1/deliveries \
  -d '{"orderId":"test","idempotencyKey":"unique-123",...}'
# Response: {"deliveryId":"abc123",...}  # SAME - not duplicated!
```

---

## 🔍 Debugging

### Check Service Health
```bash
curl http://localhost:8081/health
# Response: 200 OK
```

### View API Documentation
```
http://localhost:8081/scalar/v1  # Interactive API docs
```

### Check Logs
```bash
docker-compose logs -f mock-delivery
# Should see: Delivery {id} transitioned from Created to PickedUp
```

### Common Issues

**Issue**: Webhooks not being received  
**Solution**: 
- Check webhook URL is accessible from container
- Use `http://host.docker.internal:5000` for localhost on Windows/Mac
- Use actual IP on Linux

**Issue**: Statuses not changing  
**Solution**:
- Check `StatusTransitionDelaySeconds` setting
- Verify background worker is running: `docker logs mock-delivery | grep "Simulation Worker"`

**Issue**: "DeliveryId not found"  
**Solution**:
- Make sure delivery was created successfully (201 response)
- Check deliveryId in response vs your request

---

## 🏗️ Architecture for Your Project

### Recommended Flow

```
User Orders Pizza
        ↓
[Pizza API] calls [Mock Delivery]
  POST /deliveries
        ↓
[Pizza API] receives deliveryId
        ↓
[Pizza API] saves to database
        ↓
[Pizza Frontend] polls: GET /deliveries/{id} OR
[Pizza Frontend] listens to websocket for delivery status
        ↓
[Mock Delivery] updates status every 30s
        ↓
[Mock Delivery] POSTs webhook to [Pizza API]
        ↓
[Pizza API] updates database
        ↓
[Pizza Frontend] updates UI (via websocket or refresh)
        ↓
Status: Delivered ✅
```

---

## 📚 Key Concepts

### Idempotency
Send the same `idempotencyKey` → Same result. Safe for retries.

### Webhooks
Service pushes updates to you (not the other way around). More efficient than polling.

### Status Transitions
Automatic progression: Created → PickedUp → OnTheWay → Delivered

### Failure Handling
- Random failures (configurable percentage)
- Forced failures (for testing)
- Webhook retries with exponential backoff

---

## 🔗 Integration Patterns

### Pattern 1: Database-Backed Orders
```csharp
// When user orders
var deliveryId = await deliveryService.CreateDelivery(order);
order.DeliveryId = deliveryId;
await orderRepo.SaveAsync(order);

// When webhook comes in
var order = await orderRepo.GetByDeliveryIdAsync(webhook.DeliveryId);
order.DeliveryStatus = webhook.Status;
await orderRepo.UpdateAsync(order);
```

### Pattern 2: Event-Driven
```csharp
// Publish event on successful delivery creation
await eventBus.PublishAsync(new DeliveryCreatedEvent { 
    DeliveryId = delivery.Id, 
    OrderId = order.Id 
});

// Subscribe to delivery status changes
eventBus.Subscribe<DeliveryStatusChangedEvent>(async e => {
    await orderService.UpdateOrderStatusAsync(e.OrderId, e.Status);
    await notificationService.NotifyCustomerAsync(e.OrderId);
});
```

### Pattern 3: Real-Time WebSocket
```javascript
// Frontend connects to backend WebSocket
ws.on('delivery-updated', (delivery) => {
  displayDeliveryStatus(delivery.status);
  showDriverLocation(delivery.driver);
});

// Backend receives webhook, broadcasts to all clients
app.post('/webhooks/delivery', (req, res) => {
  io.to(req.body.orderId).emit('delivery-updated', req.body);
  res.ok();
});
```

---

## ✅ Integration Checklist

- [ ] Service is running (docker-compose up)
- [ ] Create delivery endpoint working
- [ ] Webhook endpoint created and tested
- [ ] Database schema updated (add DeliveryId, DeliveryStatus fields)
- [ ] Webhook handler processes updates correctly
- [ ] Frontend displays delivery status
- [ ] Error handling for webhook failures
- [ ] Idempotency implemented (if using .NET HttpClient, include idempotencyKey)
- [ ] Logging configured
- [ ] Tests passing

---

## 🚀 Production Deployment

```bash
# Pull latest image
docker pull ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest

# Push to your registry
docker tag ghcr.io/entelect-incubator/pezzza-mock-delivery-service:latest \
  your-registry.azurecr.io/mock-delivery:latest
docker push your-registry.azurecr.io/mock-delivery:latest

# Deploy with your orchestration tool
kubectl apply -f deployment.yaml
```

---

## 📞 Support

- GitHub: https://github.com/entelect-incubator/Pezzza-Mock-Delivery-Service
- Issues: Report bugs on GitHub Issues
- Training: See TRAINING.md for architecture deep-dive
- OpenAPI: http://localhost:8081/scalar/v1 (when running)

---

**This service is designed for enterprise training. Use it freely in your projects!** 🚀

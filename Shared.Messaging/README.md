# Icot.Shared.Messaging

`Icot.Shared.Messaging` provides standardized messaging, queuing, and event-bus infrastructure configurations for the ICOT Microservices ecosystem using **MassTransit** and **RabbitMQ**.

> **Note on Event Definitions**: This package is purely infrastructural. It does **not** contain specific event models or message classes (such as `WorkspaceEventMessage` or `AccountPermissionSyncEvent`). In accordance with domain-driven design, individual microservices should declare and manage their own message contracts to maintain bounded context purity.

---

## 📦 Features & Capabilities

### 1. MassTransit Configuration Boilerplate
Reduces the complex configuration of MassTransit into simple extension methods, applying ICOT's standard RabbitMQ cluster settings, connection resilience, and queue naming conventions automatically.

### 2. Raw JSON Serialization Integration
ICOT embraces polyglot communication and external service integrations. This package forces MassTransit to use **Raw JSON Serialization** (`cfg.UseRawJsonSerializer()`), dropping the strict MassTransit message envelop (`urn:message:...`) wrappers. 
- **Benefit**: Other tech stacks (Node.js, Go, Python) can publish and consume messages using plain JSON without understanding MassTransit's internal formatting.

### 3. Environment Variable Binding
Automatically hooks into standard ICOT environment variables (`RABBITMQ_HOST`, `RABBITMQ_USERNAME`, `RABBITMQ_PASSWORD`) avoiding hardcoded configurations in `appsettings.json`.

---

## 🚀 Quick Start Guide

### Setup Messaging in a Service

In your service's `Program.cs` or infrastructure layer, use the shared extensions to register your publishers and consumers.

```csharp
using Shared.Kernel.Services; // RabbitMqExtensions

var builder = WebApplication.CreateBuilder(args);

// Register MassTransit with standardized RabbitMQ settings
builder.Services.AddPlatformMassTransit(cfg => 
{
    // Register your service-specific consumers
    cfg.AddConsumer<MyServiceEventConsumer>();

    // Configure the RabbitMQ host and receive endpoints
    cfg.UsingPlatformRabbitMq((context, rabbitCfg) => 
    {
        rabbitCfg.ReceiveEndpoint(RabbitMqQueues.MY_SERVICE_QUEUE, e =>
        {
            e.ConfigureConsumer<MyServiceEventConsumer>(context);
        });
    });
});
```

### Defining Your Own Messages
Always define your message contracts within the specific service that owns the context.

```csharp
// Inside MyService.Application/Messages/MyEventMessage.cs
public class MyEventMessage
{
    [JsonPropertyName("event")]
    public string Event { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}
```

### Publishing Raw JSON Events
Because of the `UseRawJsonSerializer` configuration, you can use anonymous objects to publish cross-service messages without needing to share class libraries:

```csharp
var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:icot_other_service_queue"));

await endpoint.Send(new 
{ 
    @event = "entity:created",
    priority = 4,
    data = new { id = entityId, name = entityName }
});
```

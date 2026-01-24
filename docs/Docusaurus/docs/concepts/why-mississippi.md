---
sidebar_position: 3
title: Why Mississippi?
description: Benefits for enterprise teams compared to traditional 3-tier architectures
---

Mississippi is designed for **enterprise teams** building event-sourced
applications with real-time UX requirements. This page explains why you might
choose Mississippi over a traditional 3-tier architecture or a custom CQRS/ES
implementation.

## Traditional 3-Tier vs Mississippi

A traditional 3-tier web application separates presentation, business logic,
and data access. Mississippi extends this model with event sourcing and
real-time updates:

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                     TRADITIONAL 3-TIER                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌───────────────┐   ┌───────────────┐   ┌───────────────┐                 │
│   │ Presentation  │──▶│Business Logic │──▶│  Data Access  │                 │
│   │   (UI/API)    │   │   (Services)  │   │   (DB/ORM)    │                 │
│   └───────────────┘   └───────────────┘   └───────────────┘                 │
│                                                                             │
│   • Request/response only                                                   │
│   • Mutable state in database                                               │
│   • Polling or webhooks for updates                                         │
│   • Manual read model synchronization                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                        MISSISSIPPI                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌───────────────┐   ┌───────────────┐   ┌───────────────┐                 │
│   │    Client     │◀─▶│    Server     │◀─▶│     Silo      │                 │
│   │  (Reservoir)  │   │  (API + Hub)  │   │   (Orleans)   │                 │
│   └───────────────┘   └───────────────┘   └───────────────┘                 │
│          │                   │                   │                          │
│          ▼                   ▼                   ▼                          │
│   ┌───────────────┐   ┌───────────────┐   ┌───────────────┐                 │
│   │  Inlet Push   │   │   Aqueduct    │   │ Brooks/Events │                 │
│   │  (Real-time)  │   │  (Backplane)  │   │  (Immutable)  │                 │
│   └───────────────┘   └───────────────┘   └───────────────┘                 │
│                                                                             │
│   • Bi-directional real-time updates                                        │
│   • Immutable event log (full history)                                      │
│   • Automatic projection synchronization                                    │
│   • CQRS separation of reads and writes                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Key Differences

| Aspect | Traditional 3-Tier | Mississippi |
| --- | --- | --- |
| **State model** | Mutable rows in DB | Immutable event log |
| **History** | Lost on update | Complete audit trail |
| **Read/write** | Same model | Separated (CQRS) |
| **Client updates** | Poll or webhook | Real-time push |
| **Scaling** | Vertical + read replicas | Orleans virtual actors |
| **Projections** | Manual sync | Automatic from events |

## Enterprise Benefits

### 1. Speed of Development

Mississippi lets you **focus on business logic**. Once you define your domain
schema—aggregates, events, and projections—the framework handles:

- Event persistence and replay
- Projection updates and caching
- Real-time client delivery
- SignalR connection management
- State synchronization

**You write the "what" (business rules), Mississippi handles the "how"
(infrastructure).**

```csharp
// Define an aggregate with commands and events
[GenerateAggregateEndpoints]
public class OrderAggregate : AggregateBase<OrderState>
{
    [GenerateCommand]
    public async Task PlaceOrder(PlaceOrderCommand cmd)
    {
        if (State.IsPlaced) throw new DomainException("Already placed");
        await Emit(new OrderPlacedEvent(cmd.OrderId, cmd.Items));
    }
}

// That's it—endpoints, client state, and real-time updates are generated
```

### 2. Schema-Driven Development

With source generators, Mississippi supports a **spec-first workflow**:

1. Define your domain types (commands, events, projections)
2. Annotate with generator attributes
3. Build → endpoints, DTOs, and client state appear
4. Implement business rules in aggregate handlers

This approach works well for teams that start with domain modeling or API
specifications before writing implementation code.

### 3. Own Your Code

Mississippi uses **source generators, not runtime magic**. The generated code
is:

- Visible in your project (under `obj/Generated/`)
- Debuggable with step-through
- Replaceable if you need custom behavior

**You can opt out of generators entirely.** If a generated endpoint doesn't fit
your needs, write a manual controller—the framework doesn't require code
generation to function.

```csharp
// Option A: Use generated endpoints
[GenerateAggregateEndpoints]
public class OrderAggregate { ... }

// Option B: Write manual endpoints when you need control
[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private IGrainFactory Grains { get; }

    [HttpPost("{id}/place")]
    public async Task<IActionResult> PlaceOrder(string id, PlaceOrderRequest req)
    {
        var grain = Grains.GetGrain<IOrderAggregate>(id);
        await grain.PlaceOrder(new PlaceOrderCommand(id, req.Items));
        return Ok();
    }
}
```

### 4. Adopt Quickly, Extend Later

Mississippi is designed for **incremental adoption**:

| Phase | Approach | Effort |
| --- | --- | --- |
| **Proof of concept** | Use all generators, default providers | Days |
| **MVP** | Customize select endpoints, keep defaults | Weeks |
| **Production** | Replace providers, add custom behaviors | As needed |

You can ship a working prototype in days using conventions, then incrementally
replace generated code with custom implementations as requirements evolve.

### 5. Opinionated and Consistent

Mississippi is **deliberately opinionated**. Developers who learn the patterns
on one project can apply them immediately to another:

- Aggregates always handle commands and emit events
- Brooks always store events immutably
- Projections always subscribe to events and rebuild from scratch
- Reservoir always uses actions, reducers, and effects
- Inlet always manages subscriptions and pushes updates

**The patterns are the same across every Mississippi project.** This reduces
onboarding time and enables team mobility.

### 6. Pluggable Providers

All external dependencies are **abstracted behind interfaces**:

| Component | Interface | Default Provider | Alternatives |
| --- | --- | --- | --- |
| Event storage | `IBrookStorageProvider` | Cosmos DB | SQL Server, PostgreSQL |
| Snapshots | `ISnapshotStorageProvider` | Cosmos DB | S3, file system |
| Serialization | `IEventSerializer` | System.Text.Json | MessagePack, custom |

To swap a provider, implement the interface and register it:

```csharp
// Start with Cosmos DB
services.AddCosmosBrookStorage(options => ...);

// Later, migrate to PostgreSQL
services.AddSingleton<IBrookStorageProvider, PostgresBrookStorageProvider>();
```

This means your business logic is **decoupled from infrastructure choices**.
You can change storage backends without rewriting aggregates or projections.

## When to Use Mississippi

Mississippi is a good fit when:

- ✅ You need **real-time updates** in the browser
- ✅ You want **full audit history** of all changes
- ✅ You're building **collaborative or multi-user** features
- ✅ You need **CQRS separation** of reads and writes
- ✅ You want **Orleans virtual actors** for scaling
- ✅ You value **convention over configuration**

Mississippi may not be the best fit when:

- ❌ Simple CRUD with no real-time requirements
- ❌ Read-heavy workloads with no write separation
- ❌ Teams unfamiliar with event sourcing concepts
- ❌ Projects that need maximum flexibility over convention

## Comparison Summary

| Factor | Traditional | Custom CQRS/ES | Mississippi |
| --- | --- | --- | --- |
| **Time to MVP** | Fast | Slow | Fast |
| **Real-time** | Manual | Manual | Built-in |
| **Event sourcing** | No | Yes | Yes |
| **Learning curve** | Low | High | Medium |
| **Flexibility** | High | High | Medium-High |
| **Consistency** | Varies | Varies | Enforced |
| **Code ownership** | Full | Full | Full |

## Related Topics

- [Architecture](./architecture.md) — Deployment stack and data flow
- [Start Here](../index.md) — Framework overview
- [Components](../platform/index.md) — Component-by-component tour

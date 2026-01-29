---
id: attributes
title: Inlet Attributes
sidebar_label: Attributes
sidebar_position: 3
description: Complete reference for all Inlet code generation attributes.
---

# Inlet Attributes

## Overview

Inlet uses attributes to identify domain types that should trigger code generation. This page summarizes the available attributes and the properties they expose.

## Aggregate Attributes

### GenerateAggregateEndpointsAttribute

Marks an aggregate record for infrastructure code generation across all tiers.

```csharp
[GenerateAggregateEndpoints]
[GenerateAggregateEndpoints(FeatureKey = "customKey", RoutePrefix = "custom-prefix")]
```

([GenerateAggregateEndpointsAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs))

| Property | Default | Description |
|----------|---------|-------------|
| `FeatureKey` | camelCase of aggregate name minus "Aggregate" suffix | Key used for client-side feature state naming |
| `RoutePrefix` | Kebab-case of aggregate name minus "Aggregate" suffix | URL prefix for API endpoints (e.g., `bank-account`) |

**Generated Code:**

| Tier | Generated Output |
|------|------------------|
| **Inlet.Silo.Generators** | Registration extensions for aggregates and projections |
| **Inlet.Server.Generators** | Controller with endpoints for each command marked with `[GenerateCommand]` |
| **Inlet.Client.Generators** | Feature state, reducers, and registration for client command handling |

**Example:**

```csharp
[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTSTATE")]
[GenerateAggregateEndpoints(FeatureKey = "bankAccount")]
[GenerateSerializer]
public sealed record BankAccountAggregate
{
    // Properties...
}
```

## Command Attributes

### GenerateCommandAttribute

Marks a command record for endpoint code generation.

```csharp
[GenerateCommand]
[GenerateCommand(Route = "custom-route", HttpMethod = "PUT")]
```

([GenerateCommandAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateCommandAttribute.cs))

| Property | Default | Description |
|----------|---------|-------------|
| `Route` | Kebab-case of command name | HTTP route segment for this command |
| `HttpMethod` | `POST` | HTTP method (`POST`, `PUT`, `DELETE`, etc.) |

**Requirements:**

- Command must be in a `Commands` sub-namespace of an aggregate with `[GenerateAggregateEndpoints]`

**Generated Code:**

| Tier | Generated Output |
|------|------------------|
| **Server** | `{Command}Dto` — Request DTO |
| **Server** | `{Command}DtoMapper` — Maps DTO to domain command |
| **Server** | Controller action with `[HttpPost("{Route}")]` (or specified method) |
| **Client** | `{Command}Action` — Command action record with `EntityId` |
| **Client** | `{Command}ExecutingAction`, `{Command}SucceededAction`, `{Command}FailedAction` — Lifecycle actions |
| **Client** | `{Command}RequestDto` — HTTP request body DTO |
| **Client** | `{Command}ActionEffect` — HTTP POST effect handler |

**Route Pattern:**

```text
{HttpMethod} /api/aggregates/{aggregate}/{entityId}/{Route}
```

**Example:**

```csharp
namespace Spring.Domain.Aggregates.BankAccount.Commands;

[GenerateCommand(Route = "deposit")]
[GenerateSerializer]
public sealed record DepositFunds
{
    [Id(0)]
    public decimal Amount { get; init; }
}
// Generates: POST /api/aggregates/bank-account/{entityId}/deposit
```

## Projection Attributes

### GenerateProjectionEndpointsAttribute

Marks a projection record for read endpoint code generation.

```csharp
[GenerateProjectionEndpoints]
[GenerateProjectionEndpoints(GenerateClientSubscription = false)]
```

([GenerateProjectionEndpointsAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateProjectionEndpointsAttribute.cs))

| Property | Default | Description |
|----------|---------|-------------|
| `GenerateClientSubscription` | `true` | Whether to generate client-side SignalR subscription code |

**Requirements:**

- Projection must also have `[ProjectionPath]` applied

**Generated Code:**

| Tier | Generated Output |
|------|------------------|
| **Inlet.Silo.Generators** | Registration extensions for projections |
| **Inlet.Server.Generators** | `{Projection}Controller` — Read-only GET endpoint |
| **Inlet.Server.Generators** | `{Projection}Dto` — Response DTO |
| **Inlet.Server.Generators** | `{Projection}Mapper` — Maps projection to DTO |

### ProjectionPathAttribute

Defines the path-based addressing scheme for projection subscriptions.

```csharp
[ProjectionPath("bank-account-balance")]
[ProjectionPath("inventory/products")]
```

([ProjectionPathAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Abstractions/ProjectionPathAttribute.cs))

| Parameter | Description |
|-----------|-------------|
| `path` | Base path in `{feature}/{module}` format |

**Usage:**

The path is used for:

1. **API routes**: `GET /api/projections/{path}/{entityId}`
2. **SignalR subscriptions**: Subscribe to `{path}` with entity ID

**Full Entity Path:**

```text
{path}/{entityId}
```

For example: `bank-account-balance/account-123`

**Example:**

```csharp
[ProjectionPath("bank-account-balance")]
[BrookName("SPRING", "BANKING", "ACCOUNT")]
[GenerateProjectionEndpoints]
public sealed record BankAccountBalanceProjection
{
    // Properties...
}
// API: GET /api/projections/bank-account-balance/{entityId}
// SignalR: Subscribe("bank-account-balance", "entity-id")
```

## Property Attributes

### GeneratorPropertyNameAttribute

Overrides the property name in generated DTOs.

```csharp
[GeneratorPropertyName("customName")]
```

([GeneratorPropertyNameAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GeneratorPropertyNameAttribute.cs))

### GeneratorRequiredAttribute

Marks a property as required in generated DTOs.

```csharp
[GeneratorRequired]
```

([GeneratorRequiredAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GeneratorRequiredAttribute.cs))

### GeneratorIgnoreAttribute

Excludes a property from generated code.

```csharp
[GeneratorIgnore]
```

([GeneratorIgnoreAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GeneratorIgnoreAttribute.cs))

## Serialization Attributes

The Spring sample uses Orleans serialization attributes alongside Inlet attributes. For example, aggregates include `[GenerateSerializer]`, `[Id]`, and `[Alias]` in the sample domain project.

([BankAccountAggregate.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs))

## Attribute Combinations

### Complete Aggregate Example

```csharp
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Generators.Abstractions;
using Orleans;

namespace Spring.Domain.Aggregates.BankAccount;

[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTSTATE")]
[GenerateAggregateEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.BankAccountAggregate")]
public sealed record BankAccountAggregate
{
    [Id(0)]
    public decimal Balance { get; init; }
    
    [Id(1)]
    public bool IsOpen { get; init; }
}
```

### Complete Command Example

```csharp
using Mississippi.Inlet.Generators.Abstractions;
using Orleans;

namespace Spring.Domain.Aggregates.BankAccount.Commands;

[GenerateCommand(Route = "deposit")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.DepositFunds")]
public sealed record DepositFunds
{
    [Id(0)]
    public decimal Amount { get; init; }
}
```

### Complete Projection Example

```csharp
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;
using Orleans;

namespace Spring.Domain.Projections.BankAccountBalance;

[ProjectionPath("bank-account-balance")]
[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTBALANCE")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection")]
public sealed record BankAccountBalanceProjection
{
    [Id(0)]
    public decimal Balance { get; init; }
    
    [Id(1)]
    public string HolderName { get; init; } = string.Empty;
}
```

## Summary

- Use Inlet attributes to control aggregate, command, and projection code generation.
- `GenerateCommandAttribute` defines routes and HTTP methods for command endpoints.
- `ProjectionPathAttribute` sets the projection path used by HTTP and SignalR.

## Next Steps

- [Source Generation](./source-generation.md) — See what code these attributes produce
- [Client Aggregates](./client-aggregates.md) — Using generated client-side actions


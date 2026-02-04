---
id: brook-keys
title: Brook Keys
sidebar_label: Brook Keys
sidebar_position: 2
description: Composite key structure for identifying brooks and querying event ranges.
---

# Brook Keys

## Overview

Brook keys are composite identifiers that uniquely identify an event stream within Mississippi. Each key combines a brook name (defining the stream type) with an entity identifier (selecting a specific instance). Keys enable grain routing and storage partitioning.

This page focuses on **Public API / Developer Experience**.

## Key Types

| Type | Purpose |
|------|---------|
| [`BrookKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookKey.cs) | Primary identifier combining brook name and entity ID. |
| [`BrookRangeKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookRangeKey.cs) | Extended key adding start position and count for range queries. |
| [`BrookAsyncReaderKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookAsyncReaderKey.cs) | Key for async reader grains with unique suffix for single-use semantics. |

## BrookKey Structure

A `BrookKey` consists of two string components:

| Property | Description |
|----------|-------------|
| `BrookName` | Hierarchical stream type identifier (e.g., `APP.MODULE.AGGREGATE`). |
| `EntityId` | Unique identifier for the specific entity instance. |

The composite key is serialized as `brookName|entityId` with a maximum combined length of 4192 characters.

([BrookKey source](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookKey.cs#L13-L55))

## Creating Brook Keys

### From BrookNameAttribute

The recommended approach uses `[BrookName]` attributes on grain or projection types:

```csharp
// Define the brook name on your aggregate or projection type
[BrookName("MYAPP", "ORDERS", "ORDER")]
public sealed class OrderAggregateGrain : GenericAggregateGrain<OrderState>
{
    // Implementation
}

// Create a key from the type and entity ID
BrookKey key = BrookKey.ForGrain<OrderAggregateGrain>("order-123");
// Result: BrookName = "MYAPP.ORDERS.ORDER", EntityId = "order-123"

// Alternative using any decorated type
BrookKey key2 = BrookKey.ForType<OrderAggregateGrain>("order-456");
```

([BrookNameAttribute source](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs))

### Direct Construction

For dynamic scenarios, construct keys directly:

```csharp
BrookKey key = new("MYAPP.ORDERS.ORDER", "order-789");
```

### Parsing from String

Keys can be parsed from their serialized form:

```csharp
BrookKey key = BrookKey.FromString("MYAPP.ORDERS.ORDER|order-123");
```

## BrookNameAttribute

The `[BrookName]` attribute enforces a hierarchical naming convention with three uppercase alphanumeric segments:

```csharp
[BrookName("APP", "MODULE", "NAME")]
```

| Segment | Purpose |
|---------|---------|
| `AppName` | Application or domain identifier. |
| `ModuleName` | Module or bounded context within the application. |
| `Name` | Specific aggregate, saga, or projection name. |

The attribute validates that each segment contains only uppercase alphanumeric characters (`A-Z`, `0-9`).

([BrookNameAttribute validation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs#L58-L69))

## BrookRangeKey

`BrookRangeKey` extends `BrookKey` with range parameters for batch reads:

| Property | Description |
|----------|-------------|
| `BrookName` | Hierarchical stream type identifier. |
| `EntityId` | Entity instance identifier. |
| `Start` | Starting position (0-based index). |
| `Count` | Number of events to read. |

```csharp
// Read events 0-99 from a brook
BrookRangeKey range = new("MYAPP.ORDERS.ORDER", "order-123", start: 0, count: 100);

// Convert from existing BrookKey
BrookKey key = new("MYAPP.ORDERS.ORDER", "order-123");
BrookRangeKey range = BrookRangeKey.ForRange(key, start: 50, count: 50);
```

([BrookRangeKey source](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookRangeKey.cs))

## Summary

Brook keys provide type-safe, validated identifiers for event streams. Use `[BrookName]` attributes on your domain types and factory methods like `BrookKey.ForGrain<T>()` to create keys consistently.

## Next Steps

- [Brook Events](./brook-events.md) - Understand the event envelope format.
- [Reading and Writing](./reading-and-writing.md) - Use keys with reader and writer grains.

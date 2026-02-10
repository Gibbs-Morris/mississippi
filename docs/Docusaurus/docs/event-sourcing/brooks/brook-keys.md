---
id: brook-keys
title: Brook Keys
sidebar_label: Brook Keys
sidebar_position: 2
description: Composite key structure for identifying brooks and querying event ranges.
---

# Brook Keys

## Overview

Brook keys are composite identifiers that uniquely identify an event stream. Each key combines a brook name (the stream type) with an entity identifier (a specific instance). Keys enable grain routing and storage partitioning.

This page focuses on **Public API / Developer Experience**.

## Key Types

| Type | Purpose |
|------|---------|
| [`BrookKey`][brookkey] | Primary identifier combining brook name and entity ID. |
| [`BrookRangeKey`][brookrangekey] | Extended key with start position and count for range queries. |
| [`BrookAsyncReaderKey`][brookasyncreaderkey] | Key for async reader grains with unique suffix for single-use semantics. |

## BrookKey

A `BrookKey` consists of two string components:

| Property | Description |
|----------|-------------|
| `BrookName` | Hierarchical stream type identifier (e.g., `MYAPP.ORDERS.ORDER`). |
| `EntityId` | Unique identifier for the specific entity instance. |

The composite key serializes as `brookName|entityId` with a maximum combined length of 4192 characters.

### Creating Keys

**From a decorated type (recommended):**

```csharp
// Type decorated with [BrookName("MYAPP", "ORDERS", "ORDER")]
BrookKey key = BrookKey.ForGrain<OrderAggregateGrain>("order-123");
// Result: BrookName = "MYAPP.ORDERS.ORDER", EntityId = "order-123"
```

**Direct construction:**

```csharp
BrookKey key = new("MYAPP.ORDERS.ORDER", "order-456");
```

**Parsing from string:**

```csharp
BrookKey key = BrookKey.FromString("MYAPP.ORDERS.ORDER|order-789");
```

## BrookNameAttribute

The `[BrookName]` attribute enforces a hierarchical naming convention:

```csharp
[BrookName("APP", "MODULE", "NAME")]
public sealed class MyAggregateGrain : AggregateGrainBase<MyState> { }
```

| Segment | Purpose |
|---------|---------|
| `AppName` | Application or domain identifier. |
| `ModuleName` | Module or bounded context. |
| `Name` | Specific aggregate, saga, or projection name. |

Each segment must contain only uppercase alphanumeric characters (`A-Z`, `0-9`). The attribute produces a brook name in the format `{AppName}.{ModuleName}.{Name}`.

## BrookRangeKey

`BrookRangeKey` extends `BrookKey` with range parameters for batch reads:

| Property | Description |
|----------|-------------|
| `BrookName` | Hierarchical stream type identifier. |
| `EntityId` | Entity instance identifier. |
| `Start` | Starting position (0-based index). |
| `Count` | Number of events to read. |

```csharp
// Create directly
BrookRangeKey range = new("MYAPP.ORDERS.ORDER", "order-123", start: 0, count: 100);

// Convert from existing BrookKey
BrookKey key = new("MYAPP.ORDERS.ORDER", "order-123");
BrookRangeKey range = BrookRangeKey.ForRange(key, start: 50, count: 50);
```

## Summary

Brook keys provide type-safe, validated identifiers for event streams. Use `[BrookName]` attributes on your domain types and factory methods like `BrookKey.ForGrain<T>()` to create keys consistently.

## Next Steps

- [Brook Events](./brook-events.md) - Understand the event envelope format.
- [Reading and Writing](./reading-and-writing.md) - Use keys with reader and writer grains.

[brookkey]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookKey.cs
[brookrangekey]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookRangeKey.cs
[brookasyncreaderkey]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookAsyncReaderKey.cs

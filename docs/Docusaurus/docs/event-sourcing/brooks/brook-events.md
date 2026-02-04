---
id: brook-events
title: Brook Events
sidebar_label: Brook Events
sidebar_position: 3
description: Event envelope format containing metadata and binary payload.
---

# Brook Events

## Overview

`BrookEvent` is the envelope type that wraps every event persisted to a brook. It contains metadata describing the event and a binary payload holding the serialized domain event.

This page focuses on **Public API / Developer Experience**.

## Event Structure

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier for this event instance. |
| `EventType` | `string` | Semantic type used to deserialize the payload. |
| `Source` | `string` | Logical source (typically the brook name). |
| `Time` | `DateTimeOffset?` | Timestamp when the event occurred. |
| `DataContentType` | `string` | MIME type of the payload (e.g., `application/json`). |
| `Data` | `ImmutableArray<byte>` | Serialized event payload. |
| `DataSizeBytes` | `long` | Denormalized payload size for efficient queries. |

([BrookEvent source][brookevent])

## Creating Events

```csharp
BrookEvent evt = new()
{
    Id = Guid.NewGuid().ToString(),
    EventType = "OrderPlaced",
    Source = "MYAPP.ORDERS.ORDER",
    Time = DateTimeOffset.UtcNow,
    DataContentType = "application/json",
    Data = JsonSerializer.SerializeToUtf8Bytes(orderPlacedEvent).ToImmutableArray(),
    DataSizeBytes = payload.Length
};
```

## Event Metadata

### EventType

The `EventType` property identifies how to interpret the payload. Common patterns:

- Short semantic names: `OrderPlaced`
- Fully qualified type names: `MyApp.Orders.Events.OrderPlaced`
- Versioned names: `OrderPlaced.v2`

### Source

The `Source` property typically contains the brook name that produced the event, enabling downstream consumers to filter by origin.

### DataContentType

| Value | Description |
|-------|-------------|
| `application/json` | JSON-serialized payload (default). |
| `application/octet-stream` | Binary payload with custom encoding. |

## BrookPosition

Each event in a brook has a position represented by [`BrookPosition`][brookposition]:

| Property | Description |
|----------|-------------|
| `Value` | 0-based index within the brook. `-1` indicates not set. |
| `NotSet` | Returns `true` if `Value` is `-1`. |

Positions are monotonically increasing. The first event has position `0`, the second has position `1`, and so on.

```csharp
BrookPosition position = new(42);
Console.WriteLine(position.Value);  // 42

BrookPosition empty = new();
Console.WriteLine(empty.NotSet);    // true
```

## Orleans Serialization

`BrookEvent` and `BrookPosition` are decorated with Orleans serialization attributes:

```csharp
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Brooks.Abstractions.BrookEvent")]
public sealed record BrookEvent { ... }
```

The `[Alias]` attribute ensures stable type identity across assembly versions.

## Summary

Brook events are immutable envelopes containing metadata and a binary payload. The envelope follows CloudEvents-like semantics while using Orleans serialization for efficient grain communication.

## Next Steps

- [Reading and Writing](./reading-and-writing.md) - Persist and retrieve events.
- [Storage Providers](./storage-providers.md) - Understand how events are stored.

[brookevent]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookEvent.cs
[brookposition]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookPosition.cs

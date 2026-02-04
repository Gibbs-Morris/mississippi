---
id: reading-and-writing
title: Reading and Writing Brooks
sidebar_label: Reading and Writing
sidebar_position: 4
description: Orleans grain interfaces for appending events and reading event streams.
---

# Reading and Writing Brooks

## Overview

Mississippi provides Orleans grain interfaces for brook operations. Writer grains append events atomically, reader grains retrieve events in batches or streams, and cursor grains track the latest position. Consumers access these grains through `IBrookGrainFactory`.

This page focuses on **Public API / Developer Experience**.

## Grain Types

| Grain | Purpose |
|-------|---------|
| [`IBrookWriterGrain`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Writer/IBrookWriterGrain.cs) | Appends events to a brook with optimistic concurrency. |
| [`IBrookReaderGrain`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Reader/IBrookReaderGrain.cs) | Reads events as batches (stateless worker). |
| [`IBrookAsyncReaderGrain`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Reader/IBrookAsyncReaderGrain.cs) | Reads events as `IAsyncEnumerable` (single-use). |
| [`IBrookCursorGrain`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Cursor/IBrookCursorGrain.cs) | Returns the current brook position. |

## IBrookGrainFactory

The `IBrookGrainFactory` interface resolves grain references from brook keys:

```csharp
public interface IBrookGrainFactory
{
    IBrookWriterGrain GetBrookWriterGrain(BrookKey brookKey);
    IBrookReaderGrain GetBrookReaderGrain(BrookKey brookKey);
    IBrookAsyncReaderGrain GetBrookAsyncReaderGrain(BrookKey brookKey);
    IBrookCursorGrain GetBrookCursorGrain(BrookKey brookKey);
}
```

([IBrookGrainFactory source](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Factory/IBrookGrainFactory.cs))

## Writing Events

### Appending Events

Use `IBrookWriterGrain.AppendEventsAsync` to persist events:

```csharp
IBrookGrainFactory factory = serviceProvider.GetRequiredService<IBrookGrainFactory>();
BrookKey key = BrookKey.ForGrain<OrderAggregateGrain>("order-123");

IBrookWriterGrain writer = factory.GetBrookWriterGrain(key);

ImmutableArray<BrookEvent> events = ImmutableArray.Create(
    new BrookEvent
    {
        Id = Guid.NewGuid().ToString(),
        EventType = "OrderPlaced",
        Source = key.BrookName,
        Time = DateTimeOffset.UtcNow,
        DataContentType = "application/json",
        Data = payload.ToImmutableArray(),
        DataSizeBytes = payload.Length
    });

BrookPosition newPosition = await writer.AppendEventsAsync(events);
```

### Optimistic Concurrency

Pass an expected position for concurrency control:

```csharp
// Only append if brook is at position 5
BrookPosition newPosition = await writer.AppendEventsAsync(
    events,
    expectedCursorPosition: new BrookPosition(5));
```

If the actual position differs, the storage provider throws `OptimisticConcurrencyException`.

### Cursor Moved Events

After a successful append, the writer grain publishes a `BrookCursorMovedEvent` to an Orleans stream. Downstream consumers (projections, sagas, effects) subscribe to these events for real-time updates.

([BrookCursorMovedEvent source](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Streaming/BrookCursorMovedEvent.cs))

```mermaid
flowchart LR
    A[AppendEventsAsync] --> B[Storage Write]
    B --> C[BrookCursorMovedEvent]
    C --> D[Orleans Stream]
    D --> E[Projection Grain]
    D --> F[Saga Effect]
```

## Reading Events

### Batch Reads

`IBrookReaderGrain` is a stateless worker for parallel batch reads:

```csharp
IBrookReaderGrain reader = factory.GetBrookReaderGrain(key);

ImmutableArray<BrookEvent> events = await reader.ReadEventsBatchAsync(
    readFrom: new BrookPosition(0),
    readTo: new BrookPosition(99));
```

Omit parameters to read the full brook:

```csharp
ImmutableArray<BrookEvent> allEvents = await reader.ReadEventsBatchAsync();
```

### Streaming Reads

`IBrookAsyncReaderGrain` provides `IAsyncEnumerable` for memory-efficient streaming:

```csharp
IBrookAsyncReaderGrain asyncReader = factory.GetBrookAsyncReaderGrain(key);

await foreach (BrookEvent evt in asyncReader.ReadEventsAsync())
{
    // Process each event without loading all into memory
}
```

:::note Single-Use Semantics
Each call to `GetBrookAsyncReaderGrain` returns a unique grain instance with a random suffix in its key. The grain deactivates after enumeration completes. This design avoids Orleans' `IAsyncEnumerable` routing issues with stateless workers.
:::

([IBrookAsyncReaderGrain remarks](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Reader/IBrookAsyncReaderGrain.cs#L12-L28))

## Cursor Operations

### Get Current Position

Query the latest position without reading events:

```csharp
IBrookCursorGrain cursor = factory.GetBrookCursorGrain(key);

// Cached read (fast)
BrookPosition position = await cursor.GetLatestPositionAsync();

// Storage read (confirmed)
BrookPosition confirmed = await cursor.GetLatestPositionConfirmedAsync();
```

Use `GetLatestPositionConfirmedAsync` when you need strong consistency guarantees.

## Registration

### Silo Registration

Configure Orleans silo to support brooks:

```csharp
siloBuilder.AddEventSourcing(options =>
{
    options.OrleansStreamProviderName = "MyStreams";
});
```

The host must configure streams separately:

```csharp
siloBuilder.AddMemoryStreams("MyStreams");
siloBuilder.AddMemoryGrainStorage("PubSubStore");
```

([EventSourcingRegistrations source](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks/EventSourcingRegistrations.cs))

### Service Registration

For non-Orleans hosts, register services directly:

```csharp
services.AddEventSourcingByService();
```

## Summary

Brook grains provide a clean abstraction over event stream operations. Writers append events atomically and publish cursor updates. Readers support both batch and streaming access patterns. The factory pattern keeps grain resolution consistent and testable.

## Next Steps

- [Storage Providers](./storage-providers.md) - Configure persistence backends.
- [Brooks](./brooks.md) - Return to the overview.

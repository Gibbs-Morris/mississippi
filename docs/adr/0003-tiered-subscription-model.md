# ADR-0003: Tiered Subscription Model

**Status**: Accepted
**Date**: 2026-01-03

## Context

A common UX pattern is list-detail: a list projection contains IDs, and each visible row needs detail data. This creates a subscription explosion problem:

```
Channel List (100 IDs)
├── Row 1: ChannelProjection(ch-1) ← subscription
├── Row 2: ChannelProjection(ch-2) ← subscription
├── Row 3: ChannelProjection(ch-3) ← subscription
└── ... (97 more)
```

If a user scrolls through all 100 channels:
- 100 SignalR group subscriptions
- 100 HTTP requests
- 100 active `IRipple<T>` instances
- Server memory for 100 grain activations

This is unsustainable for large lists.

## Decision

We will implement **tiered subscriptions** via `IRipplePool<T>`:

### Tiers

| Tier | State | SignalR | Data | Transition |
|------|-------|---------|------|------------|
| **HOT** | Visible | ✅ Subscribed | ✅ In memory | Row enters viewport |
| **WARM** | Recently hidden | ❌ Unsubscribed | ✅ Cached | Row leaves viewport |
| **COLD** | Evicted | ❌ Unsubscribed | ❌ Evicted | Cache timeout expires |

### Interface

```csharp
public interface IRipplePool<T> : IAsyncDisposable where T : class
{
    IRipple<T> Get(string entityId);
    void MarkVisible(string entityId);
    void MarkHidden(string entityId);
    Task PrefetchAsync(IEnumerable<string> entityIds, CancellationToken ct = default);
    RipplePoolStats Stats { get; }
}
```

### Usage Pattern

```csharp
// List component: prefetch first page
await ChannelPool.PrefetchAsync(channelIds.Take(20));

// Row component: mark visible on mount
protected override async Task OnParametersSetAsync()
{
    channel = ChannelPool.Get(ChannelId);
    ChannelPool.MarkVisible(ChannelId);
    await channel.SubscribeAsync(ChannelId);
}

// Row component: mark hidden on dispose
public async ValueTask DisposeAsync()
{
    ChannelPool.MarkHidden(ChannelId);
}
```

### Batch Optimization

`PrefetchAsync` uses generated batch endpoints:

```http
POST /api/projections/channels/batch
Content-Type: application/json

{ "entityIds": ["ch-1", "ch-2", "ch-3", ...] }
```

Single HTTP request fetches multiple projections.

## Consequences

### Positive

- Bounded SignalR subscriptions (only visible rows)
- Efficient cache utilization (warm data stays in memory)
- Batch fetching reduces HTTP round-trips
- Smooth scrolling (data prefetched)

### Negative

- More complex lifecycle management
- Pool must coordinate with virtualization libraries
- Cache eviction tuning may be needed per use case

### Neutral

- Stats property enables debugging and monitoring
- Pattern is optional—simple cases can use `IRipple<T>` directly

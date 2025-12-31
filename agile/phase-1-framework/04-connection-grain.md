# Task 1.4: ~~Connection Grain~~ → MERGED INTO TASK 1.2

**Status**: ⬜ MERGED  
**Merged Into**: [1.2 Per-Connection Subscription Grain](./02-subscription-grain.md)

## Reason for Merge

The original design had two separate grains:
- `IUxProjectionSubscriptionGrain` - keyed per projection, tracked connections
- `IConnectionSubscriptionGrain` - keyed per connection, tracked projections

**Simplified to one grain per connection** that:
- Is keyed by ConnectionId
- Subscribes to projection streams directly
- Forwards all updates to a per-connection output stream

This removes the need for a separate connection aggregate grain. The subscription grain IS the connection grain.

## What Moved to Task 1.2

- Per-connection grain keyed by SignalR ConnectionId ✓
- Subscription state management ✓
- Stream subscription/unsubscription ✓
- `ClearAllAsync()` for disconnect cleanup ✓
- Rehydration on activation ✓

## Aggregate Effects Pattern (Deferred)

The `IAggregateEffect` pattern discussed in the original task is still valuable but can be:

1. **Implemented directly in the grain** (simpler for now)
2. **Extracted to framework** if we see the pattern repeat

For now, the subscription grain handles stream subscriptions directly without the formal effects abstraction. We can refactor to use `IAggregateEffect` later if needed.

## Original Content (For Reference)

<details>
<summary>Click to expand original task content</summary>

### Why Aggregate Pattern?

The connection grain manages state (active subscriptions) and needs to:
1. Persist subscription state for recovery
2. Execute side effects (subscribe to Orleans streams)
3. Handle lifecycle events (connect, disconnect, reconnect)

### Framework Abstractions (Deferred)

```csharp
/// <summary>
/// Represents a side effect to be executed after aggregate state is persisted.
/// Effects are executed after events are committed to the brook.
/// </summary>
public interface IAggregateEffect
{
    Task ExecuteAsync(IGrainContext context, CancellationToken ct);
}
```

This can be added to the framework later when we see the pattern repeat.

</details>

## Next Steps

Proceed to [Task 1.5](./05-notification-bridge.md) after completing Task 1.2.

```csharp
internal sealed class SubscribeToProjectionStreamEffect : IAggregateEffect
{
    public required string ProjectionKey { get; init; }
    public required string ConnectionId { get; init; }
    
    public async Task ExecuteAsync(IGrainContext context, CancellationToken ct)
    {
        var grainFactory = context.GrainFactory;
        var projectionGrain = grainFactory.GetGrain<IUxProjectionSubscriptionGrain>(ProjectionKey);
        await projectionGrain.SubscribeAsync(ConnectionId);
    }
}
```

### State

```csharp
[GenerateSerializer]
internal sealed record ConnectionSubscriptionState
{
    [Id(0)] public Dictionary<string, UxProjectionSubscriptionRequest> ActiveSubscriptions { get; init; } = [];
}
```

### Subscribe Flow

```csharp
public async Task<string> SubscribeAsync(UxProjectionSubscriptionRequest request)
{
    string subscriptionId = $"{request.ProjectionType}:{request.BrookType}:{request.EntityId}";
    
    // Idempotent: if already subscribed, just return
    if (State.ActiveSubscriptions.ContainsKey(subscriptionId))
        return subscriptionId;
    
    // Register with the projection-level subscription grain
    IUxProjectionSubscriptionGrain projectionGrain = 
        GrainFactory.GetGrain<IUxProjectionSubscriptionGrain>(subscriptionId);
    await projectionGrain.SubscribeAsync(this.GetPrimaryKeyString());
    
    // Store locally
    State.ActiveSubscriptions[subscriptionId] = request;
    await WriteStateAsync();
    
    return subscriptionId;
}
```

### Resubscribe Flow (Reconnect)

```csharp
public async Task ResubscribeAsync(ImmutableList<UxProjectionSubscriptionRequest> subscriptions)
{
    // Clear existing subscriptions first
    await ClearAllAsync();
    
    // Re-register all subscriptions
    foreach (var request in subscriptions)
    {
        await SubscribeAsync(request);
    }
}
```

### Cleanup Flow (Disconnect)

```csharp
public async Task ClearAllAsync()
{
    string connectionId = this.GetPrimaryKeyString();
    
    // Unsubscribe from all projection grains
    foreach (var subscriptionId in State.ActiveSubscriptions.Keys)
    {
        IUxProjectionSubscriptionGrain projectionGrain = 
            GrainFactory.GetGrain<IUxProjectionSubscriptionGrain>(subscriptionId);
        await projectionGrain.UnsubscribeAsync(connectionId);
    }
    
    // Clear local state
    State.ActiveSubscriptions.Clear();
    await WriteStateAsync();
}
```

## TDD Steps

1. **Red**: Create `ConnectionSubscriptionGrainTests` in `tests/EventSourcing.UxProjections.L0Tests/`
   - Test: `SubscribeAsync_AddsToLocalStateAndCallsProjectionGrain`
   - Test: `SubscribeAsync_Idempotent_ReturnsSameId`
   - Test: `UnsubscribeAsync_RemovesFromStateAndProjectionGrain`
   - Test: `ResubscribeAsync_ClearsAndRestoresAll`
   - Test: `ClearAllAsync_UnsubscribesFromAllProjections`
   - Test: `GetSubscriptionsAsync_ReturnsAllActive`

2. **Green**: Implement `ConnectionSubscriptionGrain`
   - Inherit from `Grain<ConnectionSubscriptionState>`
   - Inject `IGrainFactory` for projection grain access
   - Add logger extensions

3. **Refactor**: Consider parallel unsubscribe in `ClearAllAsync` for performance

## Files to Create

- `src/EventSourcing.UxProjections/Subscriptions/ConnectionSubscriptionGrain.cs`
- `src/EventSourcing.UxProjections/Subscriptions/ConnectionSubscriptionState.cs`
- `src/EventSourcing.UxProjections/Subscriptions/ConnectionSubscriptionGrainLoggerExtensions.cs`
- `tests/EventSourcing.UxProjections.L0Tests/Subscriptions/ConnectionSubscriptionGrainTests.cs`

## Notes

- Grain activation/deactivation naturally handles short-lived connections
- Consider TTL for orphaned grains if connection never formally disconnects
- Resubscribe is designed for client-driven reconnect where client has subscription list in memory

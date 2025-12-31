# Task 1.1: Add Subscription Abstractions

**Status**: ✅ Complete  
**Depends On**: None

## Goal

Define the core interfaces and types for the real-time projection subscription system in `src/EventSourcing.UxProjections.Abstractions/`.

## Acceptance Criteria

- [x] `IUxProjectionSubscriptionGrain` interface defined (keyed by **ConnectionId**)
- [x] `UxProjectionChangedEvent` record with Orleans serialization attributes
- [x] `UxProjectionSubscriptionRequest` record for subscription payloads
- [x] All types follow Mississippi naming conventions and Orleans serialization patterns
- [x] L0 tests verify record semantics and equality for new types

## Types to Create

### `IUxProjectionSubscriptionGrain` (Keyed by ConnectionId)

```csharp
/// <summary>
/// Grain managing all projection subscriptions for a single SignalR connection.
/// Keyed by SignalR ConnectionId.
/// </summary>
public interface IUxProjectionSubscriptionGrain : IGrainWithStringKey
{
    /// <summary>Subscribe to a projection. Returns subscription ID.</summary>
    Task<string> SubscribeAsync(UxProjectionSubscriptionRequest request);
    
    /// <summary>Unsubscribe from a specific subscription.</summary>
    Task UnsubscribeAsync(string subscriptionId);
    
    /// <summary>Returns all active subscriptions for this connection.</summary>
    [ReadOnly]
    Task<ImmutableList<UxProjectionSubscriptionRequest>> GetSubscriptionsAsync();
    
    /// <summary>Clears all subscriptions (called on disconnect).</summary>
    Task ClearAllAsync();
}
```

### `UxProjectionChangedEvent`

```csharp
/// <summary>
/// Event published when a projection's version changes.
/// Sent to subscribers via Orleans streams.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionChangedEvent")]
public sealed record UxProjectionChangedEvent
{
    [Id(0)] public required UxProjectionKey ProjectionKey { get; init; }
    [Id(1)] public required BrookPosition NewVersion { get; init; }
    [Id(2)] public required DateTimeOffset Timestamp { get; init; }
}
```

### `UxProjectionSubscriptionRequest`

```csharp
/// <summary>
/// Request to subscribe to a projection.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionSubscriptionRequest")]
public sealed record UxProjectionSubscriptionRequest
{
    [Id(0)] public required string ProjectionType { get; init; }
    [Id(1)] public required string BrookType { get; init; }
    [Id(2)] public required string EntityId { get; init; }
    
    /// <summary>Client-assigned ID for matching responses.</summary>
    [Id(3)] public required string ClientSubscriptionId { get; init; }
}
```

## TDD Steps

1. **Red**: Create test class `UxProjectionSubscriptionTypesTests` in `tests/EventSourcing.UxProjections.Abstractions.L0Tests/`
   - Test `UxProjectionChangedEvent` can be created with all properties
   - Test `UxProjectionSubscriptionRequest` equality and record semantics
   - (Serialization tests require Orleans test cluster, may defer to integration)

2. **Green**: Add types to `src/EventSourcing.UxProjections.Abstractions/`
   - Create `Subscriptions/` subfolder for organization
   - Add grain interface
   - Add event/request records with serialization attributes

3. **Refactor**: Ensure XML documentation is complete; align with existing namespace patterns

## Files to Create

- `src/EventSourcing.UxProjections.Abstractions/Subscriptions/IUxProjectionSubscriptionGrain.cs`
- `src/EventSourcing.UxProjections.Abstractions/Subscriptions/UxProjectionChangedEvent.cs`
- `src/EventSourcing.UxProjections.Abstractions/Subscriptions/UxProjectionSubscriptionRequest.cs`
- `tests/EventSourcing.UxProjections.Abstractions.L0Tests/Subscriptions/UxProjectionSubscriptionTypesTests.cs`

## Notes

- Follow existing patterns from `UxProjectionKey.cs` and `BrookPosition` for serialization
- Grain interface extends `IGrainWithStringKey` - keyed by ConnectionId
- `[ReadOnly]` attribute on query methods for Orleans reentrancy optimization

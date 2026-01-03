# ADR-0002: Attribute Placement Strategy

**Status**: Accepted
**Date**: 2026-01-03

## Context

Source generators need to discover types to generate controllers, route registries, and DI registrations. We must decide where to place attributes that trigger generation.

Aggregates and Projections have different natures:

| Aspect | Aggregate | Projection |
|--------|-----------|------------|
| What is it? | An actor that receives commands | A data product for the UI |
| Grain role | IS the domain concept | Mechanism to build data |
| State role | Internal to the grain | THE thing being exposed |
| Identity | The grain interface | The projection record |

## Decision

We will place attributes based on **what the concept fundamentally IS**:

### Aggregates: Attribute on Grain Interface

The grain interface IS the actor, so `[UxAggregate]` goes there:

```csharp
[UxAggregate(Route = "channels")]
public interface IChannelAggregateGrain : IAggregateGrain<ChannelState>
{
    [CommandRoute("create")]
    Task<OperationResult> CreateAsync(CreateChannelCommand command);
}
```

### Projections: Attribute on Projection Record

The projection record IS the data product, so `[UxProjection]` goes there:

```csharp
[UxProjection(Route = "channels", BrookName = "cascade.chat.channels")]
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELS")]
[GenerateSerializer]
public sealed record ChannelProjection
{
    [Id(0)] public required string Id { get; init; }
    [Id(1)] public required string Name { get; init; }
}
```

The grain class becomes configuration-free—it reads everything from `TProjection`'s attributes:

```csharp
public sealed class ChannelProjectionGrain 
    : UxProjectionGrainBase<ChannelProjection>
{
    // Empty! Base class reads config from ChannelProjection attributes
}
```

## Consequences

### Positive

- Single source of truth for projection configuration
- All projection metadata visible on one type
- Matches existing patterns (`[SnapshotStorageName]`, `[GenerateSerializer]` already on record)
- Generator logic simplified (scan records, not grain/interface pairs)
- Less boilerplate in grain classes

### Negative

- Different patterns for aggregates vs projections (may confuse initially)
- Requires `UxProjectionGrainBase<T>` to read attributes reflectively

### Neutral

- Analyzers enforce correct placement (RP2001: `[UxProjection]` must be on record)
- Documentation makes the reasoning clear

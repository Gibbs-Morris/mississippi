# Task 7.4: Server Implementation

**Status**: ⬜ Not Started  
**Depends On**: 7.3 Blazor Integration

## Goal

Create the `Ripples.Server` project for Blazor Server with direct Orleans grain access.

## Acceptance Criteria

- [ ] `ServerRipple<T>` implementation with direct grain calls
- [ ] `ServerRipplePool<T>` for composable projections
- [ ] `AddRipplesServer()` service registration extension
- [ ] Integration with existing `UxProjectionGrainFactory`
- [ ] In-process SignalR notification handling
- [ ] Project targets `net9.0`
- [ ] L0 tests with Orleans test cluster

## New Project

`src/Ripples.Server/Ripples.Server.csproj`

## Key Differences from Client

| Aspect | Server | Client |
|--------|--------|--------|
| Data Access | Direct grain call | HTTP request |
| Latency | Minimal | Network round-trip |
| Serialization | None (in-process) | JSON |
| SignalR | Hub context access | Client connection |

## Service Registration

```csharp
services.AddRipplesServer(options =>
{
    options.UseDirectGrainAccess = true;
});
```

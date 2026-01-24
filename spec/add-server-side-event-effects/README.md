# Add Server-Side Event Effects

**Status:** Ready for Approval (v3.1 - In-Grain Design with Observability)  
**Task Size:** Medium (simplified from Large)  
**Approval Checkpoint:** Yes (new public API/contract)

## Summary

Add support for server-side effects in aggregate grains. Effects run **inside the grain context** after events are persisted, receive an **EffectContext** (with brook info), and can **yield additional events** via `IAsyncEnumerable<object>` for streaming scenarios.

## Design Pivot (v3.1 - Final)

Major simplification from v2 fire-and-forget approach:

| Aspect | v2 (Fire-and-forget) | v3.1 (In-grain) |
|--------|---------------------|-----------------|
| Execution context | Separate StatelessWorker | **Inside aggregate grain** |
| Blocking | Non-blocking | **Blocks until complete** |
| State access | No | **No (aligned with client pattern)** |
| Context | None | **EffectContext with brook info** |
| Return type | `Task` (void) | **`IAsyncEnumerable<object>` (events)** |
| Error handling | Swallowed | **Propagates to command** |
| Observability | None | **Metrics + logging + slow warnings** |
| Use case | High-throughput background | **Data enrichment, LLM streaming** |
| Complexity | High | **Low** |

## Key Design Decisions

1. **Effects run inside the grain** - Simple mental model, blocks for consistency
2. **No state parameter** - Aligned with client-side IActionEffect pattern
3. **EffectContext with brook info** - Allows reading events if state needed (edge case)
4. **Effects block commands** - Next command waits until effects complete
5. **Effects can yield events** - `IAsyncEnumerable<object>` for streaming
6. **Comprehensive observability** - Metrics, structured logging, warnings if >1s
7. **Source generator discovers effects** - Same pattern as handlers/reducers

## Use Cases

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Data Enrichment                                          │
│    Command → Event → Effect fetches external data → Event   │
├─────────────────────────────────────────────────────────────┤
│ 2. LLM Streaming                                            │
│    Command → Event → Effect streams tokens → Events[]       │
│    (UX projections update in real-time)                     │
├─────────────────────────────────────────────────────────────┤
│ 3. Validation with External System                          │
│    Command → Event → Effect validates → ValidationEvent     │
└─────────────────────────────────────────────────────────────┘
```

## Key Links

- [implementation-plan.md](./implementation-plan.md) - **Current plan (v3.1 final)**
- [rfc.md](./rfc.md) - Design document (final)
- [learned.md](./learned.md) - Verified repository facts
- [progress.md](./progress.md) - Work log
- [reviews/](./reviews/) - Persona review documents (informed design evolution)

## Scope

### In Scope (This PR)
- `EffectContext` record with `AggregateKey`, `BrookName`, `AggregateTypeName`
- `IEventEffect<TAggregate>` interface with `IAsyncEnumerable<object>` return
- `EventEffectBase<TEvent, TAggregate>` base class
- `SimpleEventEffectBase<TEvent, TAggregate>` for effects that don't yield events
- `IRootEventEffectDispatcher<TAggregate>` for grain integration
- `EventEffectMetrics` and `EventEffectLoggerExtensions` for observability
- Source generator updates to discover `Effects/` sub-namespace
- GenericAggregateGrain modification to run effects after events
- Sample effect in Spring.Domain.Aggregates.BankAccount
- Documentation

### Out of Scope (Future PRs)
- `IAggregateCommandGateway` for saga patterns (can add later if needed)
- Fire-and-forget/background effects (different use case)
- Effect retry/resilience (users use Polly via DI)

## Design Decisions Summary

| Decision | Rationale |
|----------|-----------|
| No state parameter | Aligned with client-side IActionEffect pattern |
| EffectContext with brook info | Allows reading events if state needed (edge case) |
| Blocking execution | Orleans single-threaded grain model, transactional consistency |
| Immediate event persistence | Real-time projection updates during streaming |
| Full observability | Metrics + logging + warnings for >1s effects |
- ~~Observability infrastructure (simple logging sufficient)~~
- ~~Graceful shutdown handling~~
- ~~At-most-once semantics documentation~~

# Add Server-Side Event Effects

**Status:** Ready for Approval (v3 - In-Grain Design)  
**Task Size:** Medium (simplified from Large)  
**Approval Checkpoint:** Yes (new public API/contract)

## Summary

Add support for server-side effects in aggregate grains. Effects run **inside the grain context** after events are persisted, can **access aggregate state**, and can **yield additional events** via `IAsyncEnumerable<object>` for streaming scenarios.

## Design Pivot (v3)

Major simplification from v2 fire-and-forget approach:

| Aspect | v2 (Fire-and-forget) | v3 (In-grain) |
|--------|---------------------|---------------|
| Execution context | Separate StatelessWorker | **Inside aggregate grain** |
| Blocking | Non-blocking | **Blocks until complete** |
| State access | No | **Yes** |
| Return type | `Task` (void) | **`IAsyncEnumerable<object>` (events)** |
| Error handling | Swallowed | **Propagates to command** |
| Use case | High-throughput background | **Data enrichment, LLM streaming** |
| Complexity | High | **Low** |

## Key Design Decisions

1. **Effects run inside the grain** - Simple mental model, access to state
2. **Effects block commands** - Next command waits until effects complete
3. **Effects can yield events** - `IAsyncEnumerable<object>` for streaming
4. **Keep effects fast** - Sub-100ms guidance, not for long-running work
5. **Source generator discovers effects** - Same pattern as handlers/reducers

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

- [implementation-plan-v3.md](./implementation-plan-v3.md) - **Current plan (in-grain approach)**
- [implementation-plan-v2.md](./implementation-plan-v2.md) - Superseded (fire-and-forget)
- [implementation-plan.md](./implementation-plan.md) - Original (superseded)
- [rfc.md](./rfc.md) - Original design document
- [progress.md](./progress.md) - Work log
- [reviews/](./reviews/) - Persona review documents (informed v2, led to v3 simplification)

## Scope

### In Scope (This PR)
- `IEventEffect<TAggregate>` interface with `IAsyncEnumerable<object>` return
- `EventEffectBase<TEvent, TAggregate>` base class
- `SimpleEventEffectBase<TEvent, TAggregate>` for effects that don't yield events
- `IRootEventEffectDispatcher<TAggregate>` for grain integration
- Source generator updates to discover `Effects/` sub-namespace
- GenericAggregateGrain modification to run effects after events
- Sample effect in Spring.Domain.Aggregates.BankAccount
- Documentation

### Out of Scope (Future PRs)
- `IAggregateCommandGateway` for saga patterns (can add later if needed)
- Fire-and-forget/background effects (different use case)
- Effect retry/resilience (users use Polly via DI)

## Removed from v2

These were needed for fire-and-forget but not for in-grain:
- ~~EffectDispatcherGrain (StatelessWorker)~~
- ~~OneWay attribute / fire-and-forget~~
- ~~EffectContext record (state is available directly)~~
- ~~IdempotentEffectBase (effects are transactional with command)~~
- ~~Observability infrastructure (simple logging sufficient)~~
- ~~Graceful shutdown handling~~
- ~~At-most-once semantics documentation~~

# Add Server-Side Event Effects

**Status:** Plan Updated (Awaiting Gateway Decision)  
**Task Size:** Large  
**Approval Checkpoint:** Yes (new public API/contract, cross-component change, saga pattern foundation)

## Summary

Add support for server-side effects in aggregate grains, mirroring the existing client-side Redux effect pattern. Effects allow executing asynchronous side operations (API calls, messaging, notifications) after events are persisted.

**Key Design Decisions:**
1. Effects run via `[StatelessWorker]` grain with `[OneWay]` for throughput isolation
2. Source generator discovers effects in `Effects/` namespace (no runtime reflection)
3. Fire-and-forget execution (same pattern as snapshot persistence and client Store)
4. `EffectContext` provides rich metadata (aggregateKey, position, correlationId)
5. Full observability (metrics, structured logs, traces)

## Plan Updated (v2)

Implementation plan updated based on persona review feedback:

| Change | v1 → v2 |
|--------|---------|
| Effect resolution | ~~IEffectRegistry (reflection)~~ → **Source generator + keyed services** |
| Effect context | ~~aggregateKey string~~ → **EffectContext record** |
| Observability | ~~Minimal~~ → **Full (metrics, logs, traces)** |
| Idempotency | ~~Not addressed~~ → **Guidance + IdempotentEffectBase** |
| Graceful shutdown | ~~Not addressed~~ → **OnDeactivateAsync cancellation** |
| Documentation | ~~Basic~~ → **Comprehensive + anti-patterns** |

**Updated Plan:** [implementation-plan-v2.md](./implementation-plan-v2.md)

## Decision Needed

**IAggregateCommandGateway scope:**
- **Option A:** Include in V1 (unified mental model, enables saga orchestration)
- **Option B:** Defer to saga PR (YAGNI, reduces scope)

## Key Links

- [implementation-plan-v2.md](./implementation-plan-v2.md) - **Updated plan with feedback**
- [implementation-plan.md](./implementation-plan.md) - Original plan (superseded)
- [rfc.md](./rfc.md) - Design document with throughput and saga design
- [learned.md](./learned.md) - Verified repository facts
- [verification.md](./verification.md) - Claim verification
- [progress.md](./progress.md) - Work log
- [reviews/](./reviews/) - Persona review documents

## Resolved Questions

1. **Event Effects (not Command Effects):** Effects trigger after events are persisted for transactional safety
2. **Fire-and-forget via EffectDispatcherGrain:** Uses `[StatelessWorker]` + `[OneWay]` to avoid blocking aggregate
3. **Source generator registration:** Matches existing handler/reducer pattern (no runtime reflection)
4. **Unified Mental Model:** Client (Action→Effect) and Server (Event→Effect) follow same pattern
5. **At-most-once semantics:** Documented; IdempotentEffectBase for critical effects

## Scope

### In Scope (This PR)
- `EffectContext` record with rich metadata
- `IEventEffect<TAggregate>` and `EventEffectBase<TEvent, TAggregate>` interfaces
- `IdempotentEffectBase<TEvent, TAggregate>` optional helper
- `IEffectDispatcherGrain` with `[StatelessWorker]` + `[OneWay]`
- `EffectDispatcherMetrics` and `EffectDispatcherDiagnostics` observability
- Source generator updates to discover `Effects/` sub-namespace
- Sample effect in Spring.Domain.Aggregates.BankAccount
- Comprehensive documentation (guides, anti-patterns, testing)

### Pending Decision
- `IAggregateCommandGateway` for cross-aggregate communication

### Out of Scope (Future PRs)
- Full saga state machine framework
- Effect retry/resilience (users use Polly via DI)
- Client-side renaming (`IEffect` → `IActionEffect`)
- Effect priority/ordering attributes

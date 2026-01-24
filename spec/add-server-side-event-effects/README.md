# Add Server-Side Event Effects

**Status:** Under Review (Persona Reviews Complete)  
**Task Size:** Large  
**Approval Checkpoint:** Yes (new public API/contract, cross-component change, saga pattern foundation)

## Summary

Add support for server-side effects in aggregate grains, mirroring the existing client-side Redux effect pattern. Effects allow executing asynchronous side operations (API calls, messaging, notifications) after events are persisted.

**Key Design Decisions:**
1. Effects run via `[StatelessWorker]` grain with `[OneWay]` for throughput isolation
2. Effects can call other aggregates via `IAggregateCommandGateway` (enables saga patterns)
3. Fire-and-forget execution (same pattern as snapshot persistence and client Store)

## Reviews Complete

Four persona-based reviews have been completed:

| Persona | Verdict | Document |
|---------|---------|----------|
| Developer | ✅ Approve | [developer-review.md](./reviews/developer-review.md) |
| Architect | ⚠️ Conditional | [architect-review.md](./reviews/architect-review.md) |
| Dev Manager | ✅ Approve | [dev-manager-review.md](./reviews/dev-manager-review.md) |
| DevOps Engineer | ⚠️ Conditional | [devops-review.md](./reviews/devops-review.md) |

**Consolidated Summary:** [reviews/summary.md](./reviews/summary.md)

### Key Actions from Reviews

1. **Replace `IEffectRegistry` with .NET 8 keyed services** - Avoid reflection, AOT-compatible
2. **Add observability** - Metrics, structured logging, trace propagation required
3. **Document at-most-once semantics** - Effects can be lost during pod termination
4. **Add idempotency guidance** - Document patterns, consider base class
5. **Decision needed:** Include `IAggregateCommandGateway` now or defer to saga PR?

## Key Links

- [learned.md](./learned.md) - Verified repository facts
- [rfc.md](./rfc.md) - Design document with throughput and saga design
- [verification.md](./verification.md) - Claim verification
- [implementation-plan.md](./implementation-plan.md) - Detailed implementation steps
- [progress.md](./progress.md) - Work log
- [reviews/](./reviews/) - Persona review documents

## Resolved Questions

1. **Event Effects (not Command Effects):** Effects trigger after events are persisted for transactional safety
2. **Fire-and-forget via EffectDispatcherGrain:** Uses `[StatelessWorker]` + `[OneWay]` to avoid blocking aggregate
3. **IAggregateCommandGateway:** Effects can call other aggregates, enabling saga orchestration patterns
4. **Unified Mental Model:** Client (Action→Effect) and Server (Event→Effect) follow same pattern

## Scope

### In Scope (This PR)
- `IEventEffect<TAggregate>` and `EventEffectBase<TEvent, TAggregate>` interfaces
- `IEffectDispatcherGrain` with `[StatelessWorker]` + `[OneWay]`
- `IAggregateCommandGateway` for cross-aggregate communication
- Source generator updates to discover `Effects/` sub-namespace
- Sample effect in Spring.Domain.Aggregates.BankAccount

### Out of Scope (Future PRs)
- Full saga state machine framework
- Effect retry/resilience (users use Polly via DI)
- Client-side renaming (`IEffect` → `IActionEffect`)
- Effect priority/ordering attributes

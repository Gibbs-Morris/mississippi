# Progress Log

## 2026-01-24 (Session 2) - Throughput and Saga Research

### User Questions Addressed

User raised important design questions:
1. Should effects run in another grain context? (throughput concern)
2. Should they use `[OneWay]` calls?
3. Should effects only raise commands back to originating grain?
4. How do sagas fit in? Could aggregate grain serve as saga orchestrator?
5. Want same building blocks used throughout the app (unified mental model)

### Research Conducted

- **Orleans Reentrancy:** Grains are single-threaded, non-reentrant by default
- **Orleans [OneWay]:** Fire-and-forget attribute; caller doesn't wait
- **Orleans [StatelessWorker]:** Auto-scaling grain pool for CPU/IO work
- **Saga Patterns:** Orchestration (coordinator) vs Choreography (event-driven)
- **Existing Codebase Pattern:** `ISnapshotPersisterGrain` uses `[OneWay]` + `[StatelessWorker]`

### Key Design Decisions Made

1. **Effects run in EffectDispatcherGrain** - `[StatelessWorker]` with `[OneWay]` for throughput
2. **Fire-and-forget pattern** - Matches existing snapshot persistence and client Store
3. **IAggregateCommandGateway** - Effects can call other aggregates (saga support)
4. **Unified mental model** - Client (Action→Effect) and Server (Event→Effect) share pattern

### Updated Spec Files

- **rfc.md:** Added Effect Execution Model, Saga Design Preview, Unified Mental Model sections
- **implementation-plan.md:** Added EffectDispatcherGrain, IAggregateCommandGateway, updated phases
- **learned.md:** Added Orleans throughput patterns, existing [OneWay] usage, saga research
- **verification.md:** Added claims C11-C15, new verification questions Q15-Q23
- **README.md:** Updated status to "Ready for Review", documented resolved questions

---

## 2026-01-24 (Session 1) - Initial Research and Planning

### Explored Codebase

- **Identified key components:**
  - GenericAggregateGrain - command execution entry point
  - CommandHandlerBase / RootCommandHandler - command dispatch
  - EventReducerBase / RootReducer - event reduction
  - AggregateSiloRegistrationGenerator - DI registration codegen
  
- **Explored client-side Redux (Reservoir):**
  - IEffect interface with CanHandle + HandleAsync
  - Store.TriggerEffectsAsync runs effects after reducers
  - CommandEffectBase for HTTP command posting

### Created Spec Folder

- README.md - task overview
- learned.md - verified facts
- rfc.md - design document
- verification.md - claims and answers
- implementation-plan.md - detailed steps
- progress.md - this log

### Key Design Decision: Event Effects (not Command Effects)

Rationale:
1. Events are persisted facts; effects should react to facts
2. Command effects would run before persistence (transactional risk)
3. Aligns with event sourcing philosophy
4. Mirrors client-side pattern (actions dispatch → reducers → effects)

---

## Approval Checkpoint

**Status:** Ready for user approval

This is a large change with:
- New public API (`IEventEffect`, `IEffectDispatcherGrain`, `IAggregateCommandGateway`)
- Cross-component changes (abstractions, aggregates, generators)
- Foundation for saga patterns

**Review Points:**
1. EffectDispatcherGrain design (fire-and-forget, stateless worker)
2. IAggregateCommandGateway enabling saga patterns
3. Unified mental model across client/server
4. Implementation phases and test plan
4. Modify GenericAggregateGrain to call dispatcher
5. Update AggregateSiloRegistrationGenerator
6. Add sample effect to Spring.Domain
7. Write tests

### Status: Awaiting Approval

This is a large change with new public APIs. RFC and implementation plan are ready for review.

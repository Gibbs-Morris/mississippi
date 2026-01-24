# Progress Log

## 2026-01-24 (Session 3) - Design Pivot to In-Grain Effects

### User Request

User pivoted to a simpler approach after overnight consideration:
- Effects should run **inside the grain** (not separate StatelessWorker)
- Effects should **block the grain** (synchronous, not fire-and-forget)
- Effects should **access aggregate state** (they're in grain context)
- Effects should **yield events** via `IAsyncEnumerable<object>` (streaming support)
- Use case: Low-throughput aggregates needing data fetch/enrichment
- LLM streaming support: Effects can yield multiple events for real-time UX updates

### Key Design Changes (v2 → v3)

| Aspect | v2 | v3 |
|--------|----|----|
| Execution | Separate StatelessWorker | **In aggregate grain** |
| Blocking | Fire-and-forget | **Blocks until complete** |
| State access | No | **Yes** |
| Return type | `Task` (void) | **`IAsyncEnumerable<object>`** |
| Error handling | Swallowed | **Propagates to command** |

### Removed from v2 (Not Needed for In-Grain)

- ~~EffectDispatcherGrain (StatelessWorker)~~
- ~~OneWay attribute / fire-and-forget~~
- ~~EffectContext record (state available directly)~~
- ~~IdempotentEffectBase (effects transactional with command)~~
- ~~Observability infrastructure (simple logging sufficient)~~
- ~~Graceful shutdown handling~~
- ~~IAggregateCommandGateway (defer to saga PR)~~

### Branch Updated

- Rebased on origin/main to get IEffect → IActionEffect rename (PR #231)
- Created `implementation-plan-v3.md` with simplified in-grain design
- Updated README to reflect v3 approach
- Task size reduced from Large to Medium

---

## 2026-01-24 (Session 2 continued) - Plan Updated Based on Reviews

### Feedback Incorporated (Non-Conflicting)

All non-conflicting feedback from persona reviews has been incorporated:

| Feedback | Source | Resolution |
|----------|--------|------------|
| Replace IEffectRegistry with source generator | Architect + User | ✅ Source gen discovers effects, registers via keyed services |
| Add EffectContext for richer metadata | Developer | ✅ Record with aggregateKey, position, correlationId, etc. |
| Add observability | DevOps | ✅ EffectDispatcherMetrics + EffectDispatcherDiagnostics |
| Document at-most-once semantics | DevOps | ✅ In XML docs and guides |
| Add idempotency guidance | DevOps | ✅ IdempotentEffectBase + documentation |
| Add graceful shutdown | DevOps | ✅ OnDeactivateAsync cancellation |
| Add testing examples | Developer | ✅ Testing guide in docs |
| Add anti-patterns documentation | Dev Manager | ✅ Separate anti-patterns guide |

### Conflict Escalated to User

**IAggregateCommandGateway scope:**
- Architect: Defer to saga PR (YAGNI)
- Original design: Include for unified mental model
- Awaiting user decision

### Files Created/Updated

- `implementation-plan-v2.md` - New plan incorporating feedback
- `README.md` - Updated status, linked new plan
- `progress.md` - This update

---

## 2026-01-24 (Session 2 continued) - Persona Reviews

### User Request

User requested 4 persona-based design reviews:
1. Developer - focus on dev experience, debugging, testing
2. Architect - focus on SOLID, DRY, KISS, avoid overengineering
3. Dev Manager - focus on maintainability, long-term costs
4. DevOps Engineer - focus on scalability, pods, locks, observability

### Reviews Created

Created 5 review documents in `reviews/` folder:

1. **developer-review.md** - DX analysis
   - Key concern: Debugging fire-and-forget
   - Recommendation: Add EffectContext, testing examples, development mode

2. **architect-review.md** - SOLID/DRY/KISS analysis
   - Key concern: IEffectRegistry uses reflection
   - Recommendation: Replace with .NET 8 keyed services
   - Consider deferring IAggregateCommandGateway to saga PR

3. **dev-manager-review.md** - Maintainability analysis
   - Key concern: Documentation must ship with feature
   - Created checklist for code reviews and onboarding

4. **devops-review.md** - Operations analysis
   - Key concern: At-most-once semantics not documented
   - Recommendations: Observability, idempotency, graceful shutdown

5. **summary.md** - Consolidated findings
   - P0 items: Observability, at-most-once docs, testing examples, keyed services
   - P1 items: Idempotency guidance, graceful shutdown, gateway scope
   - Suggested implementation order

### Status Update

- Changed README status to "Under Review (Persona Reviews Complete)"
- Added review summary table to README
- Awaiting user decision on review feedback

---

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

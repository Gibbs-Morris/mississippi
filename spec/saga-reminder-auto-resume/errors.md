# Spec Review: Issues, Inconsistencies, and Potential Bugs

## Summary

Review of all files in `spec/saga-reminder-auto-resume/` cross-referenced against the actual codebase. Issues are categorized by severity.

## Resolution Summary

All 28 issues have been addressed. See inline `[RESOLVED]` tags below and the detailed changes in `progress.md`.

| # | Severity | Resolution |
|---|----------|-----------|
| 1 | Critical | `learned.md` — Reworded to "grain-blocking async", not "synchronous". Added nuance about `IAsyncEnumerable<object>` return type. |
| 2 | Critical | `rfc.md`, `code-samples.md` — Added `TSnapshot` naming convention note. Concrete type `WorldAggregate` is valid; clarified the distinction. |
| 3 | Critical | `rfc.md`, `grain-interfaces.md` — Designed `IScheduledCommandDispatcher` with pre-registered factory delegates. No `IServiceProvider` needed. |
| 4 | Critical | `verification.md` — Reconciled C5: audit state is infrastructure observability data, not domain state. C5 wording updated. |
| 5 | Significant | `rfc.md` — Added explicit section on ContinueSagaCommand/SagaOrchestrationEffect interaction (complementary, not conflicting). |
| 6 | Significant | `code-samples.md`, `rfc.md` — PaymentSagaState now implements all 6 ISagaState properties. |
| 7 | Significant | `grain-interfaces.md` — `Type CommandType` → `string CommandTypeName` throughout. Added Orleans serializability note. |
| 8 | Significant | `grain-interfaces.md` — `object` → strongly-typed `ScheduleAuditEvent` hierarchy with concrete record types. |
| 9 | Significant | `rfc.md` — Mermaid diagram fixed: "DI/startup scanning discovers attributes" not "register on aggregate grain". |
| 10 | Significant | `rfc.md` — E2→SG replaced with "command result success/failure" feedback path. Feedback is synchronous call result, not event subscription. |
| 11 | Minor | `learned.md` — Added `ScheduleBackoff` enum location note (lives in `EventSourcing.Aggregates.Abstractions`). |
| 12 | Minor | `README.md` — Added folder naming note explaining saga→aggregate scope expansion. |
| 13 | Minor | `rfc.md` — `MaxAttempts = 0` explicitly defined as "unlimited retries". Added to Resolved Decisions. |
| 14 | Minor | `verification.md` — Q2 answer re-marked as UNVERIFIED platform assumption with explanation. |
| 15 | Minor | `implementation-plan.md` — Added step 0: Orleans reminder package dependency in `Directory.Packages.props`. |
| 16 | Minor | `grain-interfaces.md` — Removed dead `IAggregateScheduleReminderGrain`. Scheduler grain implements `IRemindable` directly. |
| 17 | Minor | `code-samples.md` — `AlreadyHandled` replaced with realistic `state.LastAppliedTickToken == command.TickToken` check. |
| 18 | Minor | `code-samples.md` — Added `[BrookName("world")]` to `WorldAggregate`. Added `LastAppliedTickToken` property. |
| 19 | Minor | `implementation-plan.md` — Added `tests/EventSourcing.Aggregates.L1Tests/` for scheduler grain integration tests. |
| 20 | Minor | `progress.md` — Replaced synthetic 5-minute timestamps with honest session-level entries. |
| 21 | Minor | `rfc.md` — Added section 8: `ScheduleStartOptions` → `ScheduleRegistration` mapping with priority table. |
| 22 | Minor | `rfc.md` — Added explicit Orleans single-threaded grain concurrency note in runtime model. |
| 23 | Minor | `rfc.md` — Added full reconciliation algorithm with Mermaid flowchart in Crash Survivability section. |
| 24 | Minor | `rfc.md` — Defined `TickToken` as `"{ScheduleName}:{AggregateId}:{TickDueTimeUtc.Ticks}"` in Resolved Decisions. |
| 25 | Minor | `grain-interfaces.md` — `StopAsync` docstring now explicitly states it MUST call `UnregisterReminder`. |
| 26 | Minor | `grain-interfaces.md`, `rfc.md` — `AggregateType` derived from `BrookNameHelper.GetBrookName<TAggregate>()` with `typeof(T).Name` fallback. |
| 27 | Minor | `rfc.md` — "Log Only" audit defined as structured `LoggerExtensions` entries with specific EventIds/properties. Added to Resolved Decisions. |
| 28 | Minor | `rfc.md`, `grain-interfaces.md` — Duplicate `Name` on same aggregate causes startup exception. Validation added to implementation plan test list. |

---

## Critical Issues

### 1. `learned.md` — Incorrect claim about SagaOrchestrationEffect being "synchronous"

> "Saga orchestration currently runs as synchronous event effects in `src/EventSourcing.Sagas/SagaOrchestrationEffect.cs`."

**Actual:** `SagaOrchestrationEffect<TSaga>` implements `IEventEffect<TSaga>` whose `HandleAsync` returns `IAsyncEnumerable<object>`. The effect runs *within the grain's single-threaded context* (blocking the grain) but the handler itself is asynchronous (`async IAsyncEnumerable`). Calling this "synchronous" is misleading and could lead to incorrect assumptions about how scheduled command dispatch would interact with saga orchestration.

---

### 2. `rfc.md` / `code-samples.md` — Wrong generic type parameter name in `CommandHandlerBase`

All code samples use `CommandHandlerBase<TCommand, TAggregate>` (second parameter named `TAggregate`), e.g.:

```csharp
public sealed class SpawnUnitsTickCommandHandler
    : CommandHandlerBase<SpawnUnitsTickCommand, WorldAggregate>, ...
```

**Actual:** The real `CommandHandlerBase` is declared as `CommandHandlerBase<TCommand, TSnapshot>` (second parameter named `TSnapshot`). This is not just cosmetic — it reflects the framework's design intent that the second type parameter represents a *snapshot/state projection*, not the aggregate itself. Code samples that say `WorldAggregate` here are technically correct (concrete type), but the companion text and interface discussion never acknowledge the `TSnapshot` naming convention, which could confuse implementors about what the type parameter represents.

---

### 3. `rfc.md` / `grain-interfaces.md` — Scheduler grain uses `IServiceProvider` (violates shared policy)

`grain-interfaces.md` section 5 proposes `IScheduledCommandDispatcher` which would need to resolve command types at runtime. The RFC discusses dispatching "mapped commands" from the scheduler grain, implying the scheduler grain will need to construct/resolve command instances.

**Policy violation:** `.github/instructions/shared-policies.instructions.md` explicitly states: *"Classes MUST NOT inject `IServiceProvider` directly; use explicit dependencies or `Lazy<T>` to break circular dependencies."* The spec does not address how command construction/resolution will work without service locator. This will be flagged during implementation review.

---

### 4. `verification.md` — Claim C5 vs RFC audit model contradiction

- **C5 states:** "Framework-level scheduling should not require domain state storage in scheduler grain."
- **RFC section 6 states:** Optional `ScheduleAuditAggregate` stores durable scheduler history as aggregate events.

C5 is verified as true, but the spec then proposes `ScheduleAuditAggregate` which *is* domain-level state storage for scheduling. The verification answer for C5 says "Scheduler grain should remain infrastructure-only; business data remains in aggregate events/state" — but audit events *about scheduler lifecycle* are arguably scheduler infrastructure state being stored in an aggregate. This contradiction is never reconciled. Is audit state "business data" or "infrastructure data"? The boundary is undefined.

---

## Significant Issues

### 5. `rfc.md` / `code-samples.md` — `SagaOrchestrationEffect` uses `IEventEffect<TSaga>`, not separate command dispatch

The spec proposes that the scheduler grain dispatches a *command* to the aggregate grain (`Execute(scheduled command)`). But the current saga orchestration runs via `IEventEffect<TSaga>` (event effects), not via commands. The phase 2 design proposes `ContinueSagaCommand` as a *command*, but the spec never addresses how this interacts with the existing `SagaOrchestrationEffect` which handles orchestration via *event effects*. Would `ContinueSagaCommand` replace the effect? Supplement it? Conflict with it? This is a major architectural gap.

---

### 6. `code-samples.md` / `rfc.md` — `PaymentSagaState` code sample is inconsistent with `ISagaState`

Phase 2 saga example:

```csharp
public sealed class PaymentSagaState : ISagaState
{
    public Guid SagaId { get; init; }
    public SagaPhase Phase { get; init; }
    public int LastCompletedStepIndex { get; init; }
}
```

**Actual `ISagaState`** requires six properties: `CorrelationId`, `LastCompletedStepIndex`, `Phase`, `SagaId`, `StartedAt`, `StepHash`. The sample only implements three. This code would not compile. Every occurrence of this (both in `rfc.md` and `code-samples.md`) has the same bug.

---

### 7. `grain-interfaces.md` — `ScheduleRegistration` uses `Type` property (not serializable by Orleans)

```csharp
public sealed record ScheduleRegistration(
    ...
    Type CommandType,
    ...);
```

`System.Type` is not natively serializable by Orleans. Passing `Type` as a grain method parameter or storing it in grain state will fail at runtime unless a custom serializer is registered. The spec should use `string` (assembly-qualified name or alias) instead, consistent with how the existing framework uses `[Alias]` attributes for type identity.

---

### 8. `grain-interfaces.md` — `IScheduleAuditAggregateGrain.AppendAsync` takes `object`

```csharp
Task AppendAsync(object auditEvent, CancellationToken cancellationToken = default);
```

This is untyped. The existing framework uses strongly-typed command/event models throughout. An `object` parameter defeats Orleans serialization contracts and compile-time safety. The spec's own audit event types (`ScheduleStarted`, `ScheduleUpdated`, etc.) are never defined as concrete types in `grain-interfaces.md`, leaving the contract incomplete.

---

### 9. `rfc.md` — Mermaid sequence diagram shows `App` registering attributes on `Agg`

```
App->>Agg: Register aggregate + schedule attributes (metadata only)
```

Attributes are compile-time metadata. They are not "registered" on aggregate grains at runtime by the app. The registration would happen during DI/startup scanning. The diagram conflates compile-time attribute declaration with runtime registration, which is misleading.

---

### 10. `rfc.md` — "E2 → SG" feedback arrow in architecture diagram is undefined

```mermaid
E2 --> SG
```

The diagram shows `E2[Domain events/state updates]` flowing back to `SG[AggregateScheduleGrain]`. The RFC text never explains what feedback the scheduler grain receives from domain events. Does the scheduler subscribe to the aggregate's event stream? Does `ExecuteAsync` return a result? This is a critical runtime path detail left unspecified.

---

## Minor Issues / Inconsistencies

### 11. `learned.md` — Wrong namespace in code samples

Code samples use:

```csharp
using Mississippi.EventSourcing.Aggregates.Abstractions;
```

This is the actual namespace, so it's correct. However, the `ScheduleBackoff` enum referenced in attributes is never shown with its namespace or defined anywhere in the spec. Where does it live? `Abstractions`? A new namespace?

---

### 12. `README.md` — Title says "Aggregate Scheduled Commands" but folder slug says "saga-reminder-auto-resume"

The folder name `saga-reminder-auto-resume` suggests saga-specific scope, but the README title and scope describe a generic aggregate scheduling feature with saga as phase 2. This naming mismatch could confuse anyone browsing the spec folder.

---

### 13. `rfc.md` — `MaxAttempts = 0` is ambiguous

`AggregateScheduleDefaultsAttribute` defaults `MaxAttempts = 0`, and the "Open Decisions" section asks: "Whether max attempts default is unlimited or bounded." The attribute code already defaults to `0`, but it's never defined whether `0` means "unlimited" or "zero retries" (i.e., runs once). This ambiguity appears in the attributes, the RFC explanation, and the code samples — all using `0` without clarifying its semantics.

---

### 14. `verification.md` — Answer to Q2 is a platform assumption, not verified

> "Yes; Orleans reminders are durable with appropriate provider configuration. **Verified by platform behavior assumption**."

This is labeled "Verified" but explicitly admits it's an assumption. The spec should either cite the Orleans documentation/provider used in this repo, or mark it UNVERIFIED per the spec authoring rules.

---

### 15. `implementation-plan.md` — No mention of Orleans package dependencies

The plan touches `src/EventSourcing.Aggregates.Abstractions` for new contracts, but the scheduler grain needs `Orleans.Runtime` (for `IRemindable`, `RegisterOrUpdateReminder`, etc.). Currently, **zero files** in the entire `src/` tree reference `IRemindable` or `RegisterOrUpdateReminder`. This means Orleans reminder APIs are not used anywhere in the codebase today. The implementation plan does not acknowledge this new dependency or the impact on `Directory.Packages.props`.

---

### 16. `grain-interfaces.md` — `IAggregateScheduleReminderGrain` declared separately but never used

Section 3 defines `IAggregateScheduleReminderGrain : IGrainWithStringKey, IRemindable` and notes "this separate interface is optional." But it's never referenced by any other interface, the implementation plan, or the code samples. It's dead spec weight.

---

### 17. `code-samples.md` — `AlreadyHandled` always returns false

```csharp
private static bool AlreadyHandled(string tickToken, WorldAggregate state)
{
    return false;
}
```

The idempotency check—described as the *core safety mechanism* for duplicate ticks—is stubbed to always return false. While understood as a placeholder, it undermines the spec's emphasis on idempotency-by-design. The sample should at least show a realistic pattern (e.g., checking `state.LastAppliedTickToken`), especially since this is the DX-facing code sample document.

---

### 18. `rfc.md` — `WorldAggregate` sample lacks `[BrookName]` attribute

The existing framework requires `[BrookNameAttribute]` on aggregate state types (referenced in `IGenericAggregateGrain<TAggregate>` docs). The `WorldAggregate` sample in the RFC and `code-samples.md` omits this, making the sample incomplete for anyone following it as a template.

---

### 19. `implementation-plan.md` — Missing test project for phase 1 scheduler grain

The file/module touch list mentions `tests/EventSourcing.Aggregates.L0Tests/*` but the scheduler grain is infrastructure code in `src/EventSourcing.Aggregates`. Testing reminder-based behavior (register/tick/cancel) will likely need L1 or L2 tests with Orleans test infrastructure, which is not mentioned.

---

### 20. `progress.md` — All timestamps are within 75 minutes on the same day

All entries are from `2026-02-19T00:00:00Z` to `2026-02-19T01:15:00Z` (75 minutes). For a "Large" task with approval checkpoint, this timeline is suspiciously fast and suggests the progress log may be synthetic rather than reflecting actual work cadence. The timestamps also appear to increment in exactly 5-minute intervals, which is not realistic.

---

### 21. Cross-file inconsistency — `ScheduleStartOptions` vs `ScheduleRegistration` overlap

- `rfc.md` and `code-samples.md` show `ScheduleStartOptions` with properties like `InitialDelay` and `AuditMode`.
- `grain-interfaces.md` defines `ScheduleRegistration` with `InitialDelay`, `Interval`, `Backoff`, `MaxAttempts`, `JitterPercent`, `MaxInterval`, `AuditMode`.

The relationship between `ScheduleStartOptions` (user-facing) and `ScheduleRegistration` (grain-facing) is never defined. Does the manager merge attribute defaults with `ScheduleStartOptions` to produce a `ScheduleRegistration`? Which fields can the user override? This mapping is a critical implementation detail that's absent.

---

### 22. `rfc.md` — No concurrency control for scheduler grain itself

The RFC discusses idempotent *command handlers* extensively but never addresses concurrency on the *scheduler grain* operations. What happens if `StartScheduleAsync` and `StopScheduleAsync` are called concurrently for the same schedule? Orleans grain single-threading helps, but the spec should explicitly state this assumption if it's relying on it.

---

### 23. `rfc.md` — Reconciliation behavior described but not designed

Implementation plan step 5: "detect active schedule metadata with missing reminder; recreate reminder safely and idempotently." This is a non-trivial distributed systems problem (how does the scheduler grain know a reminder was lost vs. never registered?). The crash matrix mentions it. But no design, algorithm, or sequence diagram covers reconciliation. It's listed as "mandatory" in verification but hand-waved in implementation.

---

### 24. `rfc.md` — Ambiguity in `TickToken` Generation and Stability

The RFC and code samples rely on `TickToken` for idempotency (`SpawnUnitsTickCommand` has `TickToken`), but the spec never defines *who* generates this token or *how* it is derived.

- If the token is generated from `TickTime`, it requires `TickTime` to be perfectly stable across retries of the same tick. Orleans reminders do not guarantee exact tick time precision.
- If the token is a GUID generated by the scheduler grain, the scheduler grain must persist strictly to ensure the *same* token is used for retries of the *same* logical tick.
The generation strategy (e.g., `ScheduleName + IntervalIndex` or `DueTime.Ticks`) must be explicitly defined to ensure deterministic de-duplication.

---

### 25. `grain-interfaces.md` — `StopAsync` Persistence Behavior Unclear

The `IAggregateScheduleGrain.StopAsync` contract does not specify whether the underlying Orleans reminder row is deleted (`UnregisterReminder`) or if the schedule is just marked "disabled" in state.
- **De-registering** cleans up rows and stops billing/overhead but loses the "safety net".
- **Disabling** keeps the row but checks a flag on every tick (wasteful).
The spec should explicitly state that `StopAsync` **MUST** unregister the Orleans reminder to prevent resource leaks and cost accumulation.

---

### 26. `grain-interfaces.md` — `IAggregateScheduleManager` Generic Constraint Limitations

`StartScheduleAsync<TAggregate>` takes a `TAggregate` type parameter but the grain key is defined as `<AggregateType>|<AggregateId>|<ScheduleName>`.
- The implementation must strictly define how `AggregateType` is derived from `TAggregate`. Is it `Name`, `FullName`, or an `[Alias]`?
- If it uses `[Alias]`, the manager implementation must have access to Orleans grain type resolution logic, which is not standard in typical client-side manager implementations.
- The spec should clarify if `TAggregate` is used solely for strong typing or if it influences the grain key generation strategy.

---

### 27. `rfc.md` — Audit Mode "Log Only" Implementation Undefined

The spec proposes a "Log Only" audit mode but does not define the mechanism.
- Does "Log Only" mean writing to the Orleans `ILogger` (text logs)?
- Or does it mean emitting a different kind of event?
- If it is `ILogger`, the specific structured logging schema (EventIds, properties) should be defined to ensuring queryability, otherwise "audit" via logs is effectively impossible.

---

### 28. `rfc.md` — Duplicate Schedule Name Behavior

The `[ScheduledCommand]` attribute allows a `Name` property. The spec does not address the behavior when multiple attributes on the same aggregate class use the **same** `Name`.
- Does the last one win?
- Is it a build-time error?
- Is it a runtime startup exception?
Strict validation (throwing an exception) should be mandated to prevent silent configuration masking.


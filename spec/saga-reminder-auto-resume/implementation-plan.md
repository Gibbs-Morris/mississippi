# Implementation Plan

> Draft proposal only. Source facts were refreshed against main `c8da151e607bcc8f3b253519317a8e7418d26261` on 4 October 2026. Scheduling types shown here are proposed contracts. They are not implemented or validated by this PR. Recovery requirements remain tracked in [#404](https://github.com/Gibbs-Morris/mississippi/issues/404) and [#581](https://github.com/Gibbs-Morris/mississippi/issues/581).

## Summary

Implement a general aggregate scheduled-command framework first, then adopt it for saga auto-resume in phase 2.

Architectural policy for this feature:

- Durable state is stored via aggregate event streams.
- Infrastructure execution concerns can use separate grains.
- Scheduler grain remains control-plane; optional `ScheduleAuditAggregate` stores durable scheduler history.

## Step-by-Step Checklist

### Phase 1: Aggregate Scheduling Contracts and DX

0. **Verify existing dependencies:** `DomainModeling.Runtime.csproj` already references `Microsoft.Orleans.Reminders`; versions are central in `Directory.Packages.props`. Add or change a package only if a reviewed implementation needs it. Preserve locked restore and current main versions.

1. Add scheduling contracts in `src/DomainModeling.Abstractions`:
   - `AggregateScheduleDefaultsAttribute`
   - `ScheduledCommandAttribute`
   - `ScheduleBackoff` enum
   - optional resolved policy model.
2. Add runtime contracts:
   - scheduler registration API
   - scheduler lifecycle API (`StartSchedule`, `UpdateSchedule`, `StopSchedule`)
   - command binding metadata contract.
3. Add idempotency contracts:
   - marker/contract for idempotent scheduled command handlers
   - registration/startup validation utility.

### Phase 1.1: Runtime Infrastructure

1. Implement scheduler grain (infrastructure execution with separately owned durable control state):

   - key by `<AggregateType>|<AggregateId>|<ScheduleName>`
   - own Orleans reminder lifecycle
   - dispatch mapped command to aggregate grain on tick
   - apply backoff/jitter/max-attempt policies.
   - support multiple concurrent schedules per aggregate instance.
   - persist control generation, active/disabled intent, due identity, policy and retry/checkpoint progress in every mode; optional audit verbosity cannot gate durability.

2. Add reconciliation behavior:

   - detect active schedule metadata with missing reminder
   - define and test control-append/reminder-registration ordering, partial failures and repeated reconciliation before claiming safe recreation.

3. Ensure metadata registration does not auto-start reminders.

### Phase 1.2: Aggregate Integration

1. Add attribute scanning/registration for scheduled command bindings.
2. Add aggregate extension methods/commands to explicitly start schedules per aggregate instance.
3. Ensure terminal/disabled state can stop schedules deterministically.

### Phase 1.3: Observability

1. Add structured logging extensions for scheduling milestones:

   - schedule_created / schedule_updated / tick_triggered / tick_dispatched / schedule_canceled / schedule_exhausted

2. Add metrics counters/timers for schedule attempts, failures, and latency.

3. Add optional lifecycle events strategy:

   - default logs-only
   - opt-in standardized events for audit-sensitive domains.

4. Define `ScheduleAuditAggregate` model (opt-in):

   - event contracts (`ScheduleStarted`, `ScheduleUpdated`, `ScheduleStopped`, `TickTriggered`, `TickDispatched`, `TickFailed`, `ScheduleExhausted`)
   - aggregate key strategy (`AggregateType|AggregateId|ScheduleName`)
   - audit verbosity modes (lifecycle-only, full tick history).

### Phase 1.4: Testing

1. Add L0 tests for:

   - attribute parsing/defaults and overrides
   - no auto-start behavior
   - explicit start/stop lifecycle
   - reminder lifecycle state machine (mocked reminder service)
   - dispatch routing to correct aggregate command
   - multiple schedules per aggregate instance
   - reconciliation behavior (mocked reminder not found → re-register)
   - idempotency validation failures
   - `ScheduleStartOptions` → `ScheduleRegistration` merge logic
   - duplicate schedule name startup validation rejection
   - `MaxAttempts = 0` means unlimited; negative values rejected.

2. Add fault-injection tests for control/reminder ordering, stale generations, lost acknowledgements, no-op checkpoints, delayed/out-of-order ticks and stop races. Record the unverified cases in verification.md.

3. Add audit-mode tests:

   - audit events emitted when enabled
   - no audit events when disabled
   - correlation fields populated correctly.

4. **Add L1 tests for scheduler grain** (requires Orleans test cluster infrastructure):

   - Reminder registration/unregistration via `TestCluster` or `TestSiloBuilder`
   - Tick callback → command dispatch integration
   - Grain activation reconciliation with actual reminder service
   - Schedule start → tick → stop full lifecycle
   - Test project: `tests/DomainModeling.Runtime.L1Tests/` (new)

### Phase 2: Saga Adoption

1. Reconcile saga adoption with existing reminder ownership and #404/#581 before implementation. The similarly named command in #361 remains a separate unmerged proposal.
2. Bind saga schedules using phase 1 attributes/runtime API.
3. Define the `SagaResumeRequested` event that `ContinueSagaCommand` handler emits; ensure `SagaOrchestrationEffect.CanHandle` recognizes it.
4. Define and persist safe direction/compensation progress, workflow compatibility, stale request conflicts, bounded retry and unknown-effect intervention before enabling resume. Test partial rollback, repeated requests and lost acknowledgements.

## File/Module Touch List (Planned)

- `Directory.Packages.props` — inspect existing central dependency inputs; no automatic reminders package addition
- `src/DomainModeling.Abstractions/*` — scheduling attributes, contracts, policy models, audit event types
- `src/DomainModeling.Runtime/*` — scheduler grain, dispatcher, reconciliation, logging extensions
- `tests/DomainModeling.Runtime.L0Tests/*` — L0 unit tests (mocked dependencies)
- `tests/DomainModeling.Runtime.L1Tests/*` — L1/L2 placement selected from actual infrastructure dependencies; persistent provider and crash proof require appropriate integration coverage
- optional audit mode: `src/DomainModeling.Abstractions/*Audit*`, `src/DomainModeling.Runtime/*Audit*`
- phase 2: `src/DomainModeling.Abstractions/*`, `src/DomainModeling.Runtime/*`, `tests/DomainModeling.Runtime.L0Tests/*`

## API/Compatibility Strategy

- New contracts are additive.
- Existing aggregates remain unchanged without scheduling attributes/registration.
- Saga behavior changes only in phase 2 adoption.

## Rollout Plan

1. Ship phase 1 with opt-in aggregate attributes and runtime API.
2. Pilot on one non-saga aggregate scenario (for example periodic world tick).
3. Monitor schedule churn, duplicate dispatch handling, and latency.
4. Implement phase 2 saga adoption after phase 1 stability.

## Backout Plan

- Disable schedule activation via configuration switch.
- Keep aggregate command handling unchanged when scheduling disabled.
- Preserve idempotency checks where non-breaking.

## Validation Commands (Planned)

- Build: `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
- Cleanup: `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
- Unit tests: `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
- Routine quality: `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject DomainModeling.Runtime.L0Tests -SkipMutation`
- Full pipeline: `pwsh ./go.ps1`
- Mutation: optional under repository policy; report actual execution status and any gaps.

## Monitoring Checklist

- Active schedules by aggregate type and schedule name
- Tick dispatch success/failure ratio
- Duplicate dispatch suppression rate
- Exhausted retry count
- Schedule cancellation latency
- Audit event volume by schedule

## Risks and Mitigations

- Duplicate command execution side effects
  - Mitigation: durable per-generation duplicate decisions, delayed/out-of-order tests, downstream deduplication or reconciliation for unknown external results. Startup markers cannot prove this behavior.
- Retry storms and synchronized bursts
  - Mitigation: exponential backoff + jitter + max attempt policies.
- Missing reminder due to registration race
  - Mitigation: reconciliation and idempotent ensure-scheduled operation.
- Audit stream growth/cost
  - Mitigation: configurable verbosity and retention policy.

## Approval Required

This plan requires approval before implementation because it introduces new aggregate runtime contracts and scheduling behavior with broad framework impact.

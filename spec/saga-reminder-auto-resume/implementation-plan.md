# Implementation Plan

## Summary

Implement a general aggregate scheduled-command framework first, then adopt it for saga auto-resume in phase 2.

Architectural policy for this feature:

- Durable state is stored via aggregate event streams.
- Infrastructure execution concerns can use separate grains.
- Scheduler grain remains control-plane; optional `ScheduleAuditAggregate` stores durable scheduler history.

## Step-by-Step Checklist

### Phase 1: Aggregate Scheduling Contracts and DX

0. **Package dependency setup:**
   - Add `Microsoft.Orleans.Reminders` (or equivalent Orleans reminder package) to `Directory.Packages.props` via CPM.
   - The scheduler grain project (`src/EventSourcing.Aggregates`) will need `<PackageReference Include="Microsoft.Orleans.Reminders" />` (no `Version` attribute per CPM rules).
   - **Note:** No files in the current `src/` tree reference `IRemindable`, `RegisterOrUpdateReminder`, or `UnregisterReminder`. This is a net-new Orleans API surface for the codebase.

1. Add scheduling contracts in `src/EventSourcing.Aggregates.Abstractions`:
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

4. Implement scheduler grain (infrastructure only, no business state persistence):
   - key by `<AggregateType>|<AggregateId>|<ScheduleName>`
   - own Orleans reminder lifecycle
   - dispatch mapped command to aggregate grain on tick
   - apply backoff/jitter/max-attempt policies.
   - support multiple concurrent schedules per aggregate instance.
   - emit optional audit events to `ScheduleAuditAggregate` when audit mode is enabled.
5. Add reconciliation behavior:
   - detect active schedule metadata with missing reminder
   - recreate reminder safely and idempotently.

6. Ensure metadata registration does not auto-start reminders.

### Phase 1.2: Aggregate Integration

7. Add attribute scanning/registration for scheduled command bindings.
8. Add aggregate extension methods/commands to explicitly start schedules per aggregate instance.
9. Ensure terminal/disabled state can stop schedules deterministically.

### Phase 1.3: Observability

10. Add structured logging extensions for scheduling milestones:
   - schedule_created / schedule_updated / tick_triggered / tick_dispatched / schedule_canceled / schedule_exhausted
11. Add metrics counters/timers for schedule attempts, failures, and latency.
12. Add optional lifecycle events strategy:
   - default logs-only
   - opt-in standardized events for audit-sensitive domains.

13. Define `ScheduleAuditAggregate` model (opt-in):
   - event contracts (`ScheduleStarted`, `ScheduleUpdated`, `ScheduleStopped`, `TickTriggered`, `TickDispatched`, `TickFailed`, `ScheduleExhausted`)
   - aggregate key strategy (`AggregateType|AggregateId|ScheduleName`)
   - audit verbosity modes (lifecycle-only, full tick history).

### Phase 1.4: Testing

14. Add L0 tests for:
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
15. Add crash-matrix-driven tests for duplicate ticks, delayed ticks, and cancel race.

16. Add audit-mode tests:
   - audit events emitted when enabled
   - no audit events when disabled
   - correlation fields populated correctly.

17. **Add L1 tests for scheduler grain** (requires Orleans test cluster infrastructure):
   - Reminder registration/unregistration via `TestCluster` or `TestSiloBuilder`
   - Tick callback → command dispatch integration
   - Grain activation reconciliation with actual reminder service
   - Schedule start → tick → stop full lifecycle
   - Test project: `tests/EventSourcing.Aggregates.L1Tests/` (new)

### Phase 2: Saga Adoption

17. Add `ContinueSagaCommand` and saga-specific resume logic.
18. Bind saga schedules using phase 1 attributes/runtime API.
19. Define the `SagaResumeRequested` event that `ContinueSagaCommand` handler emits; ensure `SagaOrchestrationEffect.CanHandle` recognizes it.
20. Add saga-specific progress checkpointing and tests for compensation resume.

## File/Module Touch List (Planned)

- `Directory.Packages.props` — add Orleans reminders package
- `src/EventSourcing.Aggregates.Abstractions/*` — scheduling attributes, contracts, policy models, audit event types
- `src/EventSourcing.Aggregates/*` — scheduler grain, dispatcher, reconciliation, logging extensions
- `tests/EventSourcing.Aggregates.L0Tests/*` — L0 unit tests (mocked dependencies)
- `tests/EventSourcing.Aggregates.L1Tests/*` — **new** L1 tests with Orleans test cluster for reminder integration
- optional audit mode: `src/EventSourcing.Aggregates.Abstractions/*Audit*`, `src/EventSourcing.Aggregates/*Audit*`
- phase 2: `src/EventSourcing.Sagas.Abstractions/*`, `src/EventSourcing.Sagas/*`, `tests/EventSourcing.Sagas.L0Tests/*`

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
- Mutation: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`

## Monitoring Checklist

- Active schedules by aggregate type and schedule name
- Tick dispatch success/failure ratio
- Duplicate dispatch suppression rate
- Exhausted retry count
- Schedule cancellation latency
- Audit event volume by schedule

## Risks and Mitigations

- Duplicate command execution side effects
   - Mitigation: strict idempotency contracts + startup validation.
- Retry storms and synchronized bursts
   - Mitigation: exponential backoff + jitter + max attempt policies.
- Missing reminder due to registration race
   - Mitigation: reconciliation and idempotent ensure-scheduled operation.
- Audit stream growth/cost
   - Mitigation: configurable verbosity and retention policy.

## Approval Required

This plan requires approval before implementation because it introduces new aggregate runtime contracts and scheduling behavior with broad framework impact.

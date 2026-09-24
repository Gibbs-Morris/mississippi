# Verification

## Claim List

- C1: Aggregate scheduled commands can be a reusable framework feature beyond sagas.
- C2: Attribute-first DX on aggregate state can reduce boilerplate while preserving explicit runtime control.
- C3: Reminder lifecycle (register/update/cancel) can be made safe and deterministic.
- C4: Duplicate/delayed reminder ticks are survivable with enforced idempotency.
- C5: The scheduler grain itself must remain infrastructure-only with no domain state storage. Opt-in audit state (via `ScheduleAuditAggregate`) is *infrastructure observability data*, not domain business state, and lives in a separate dedicated aggregate stream. This distinction is explicit: domain state describes the business entity (e.g., world units, saga phase), while audit state describes scheduler operational history (e.g., tick timestamps, attempt counts).
- C6: Saga auto-resume can be implemented in phase 2 as a consumer of phase 1 primitives.
- C7: Observability must expose schedule lifecycle and execution outcomes.
- C8: Multiple reminders per aggregate instance must be supported.
- C9: Schedules must not auto-start from attributes alone.
- C10: Durable scheduler history must be stored via aggregate event streams when audit mode is enabled.
- C11: Splitting business and infrastructure grains is valid as long as durable state policy is preserved.

## Verification Questions

1. Is there a current generic aggregate scheduling feature in the repository?
2. Are Orleans reminders durable enough for this framework pattern?
3. Can attributes on aggregate state provide good defaults without forcing static-only behavior?
4. Can runtime API override attribute defaults for per-instance behavior?
5. Can one scheduler grain key (`AggregateType|AggregateId|ScheduleName`) safely isolate schedules?
6. Do duplicate/delayed ticks threaten correctness if command handlers are idempotent?
7. Should scheduler grain persist business data or remain infrastructure-only?
8. Can this design serve non-saga scenarios (for example game world ticks)?
9. Can saga resume be modeled as just another scheduled command in phase 2?
10. Is startup validation needed to enforce idempotent scheduled handlers?
11. Are logs alone enough, or are schedule lifecycle events also needed?
12. Is this an approval-checkpoint change due to broad aggregate runtime impact?
13. Can one aggregate instance run multiple independent schedules safely?
14. Should schedule activation require explicit start API/command instead of auto-start?
15. If audit durability is required, should scheduler history be persisted in a dedicated aggregate stream?
16. Is business/infrastructure grain split aligned with repository architecture rules?

## Independent Answers

1. No; there is no current generic aggregate scheduling feature. **Verified**.
2. Yes; Orleans reminders are durable when backed by a persistent provider (e.g., Azure Table Storage, ADO.NET). This repository does not currently use any reminder-related APIs (no `IRemindable`, `RegisterOrUpdateReminder`, or `UnregisterReminder` references exist in `src/`). The specific reminder provider must be configured during implementation. **UNVERIFIED — platform capability assumption; requires confirmation of which reminder storage provider will be used in this repo.**
3. Yes; attributes are suitable for defaults and discoverability. **Verified by DX analysis**.
4. Yes; runtime registration API should support override for dynamic cases. **Verified by design requirement**.
5. Yes; including schedule name in key isolates multiple schedules per aggregate instance. **Verified by key design analysis**.
6. They do not, if handlers are idempotent and terminal/disabled checks are explicit. **Verified by distributed-systems reasoning**.
7. Scheduler grain should remain infrastructure-only; business data remains in aggregate events/state. Opt-in audit state (via `ScheduleAuditAggregate`) is infrastructure observability data, not domain state, and is stored in a separate dedicated aggregate stream. This reconciles C5 with the proposed audit model. **Verified by architecture alignment; C5 clarified.**
8. Yes; game tick scenario is a direct fit for scheduled commands. **Verified by scenario mapping**.
9. Yes; `ContinueSagaCommand` can be one scheduled command binding in phase 2. **Verified by composition analysis**.
10. Yes; startup/build validation is preferred over documentation-only policy. **Verified by quality gate rationale**.
11. Recommended default is logs/metrics; standardized events should be opt-in for audit-sensitive domains. **Verified by observability trade-off**.
12. Yes; this affects core aggregate runtime and contracts. **Verified**.
13. Yes; schedule key including schedule name isolates multiple reminders safely. **Verified by key design analysis**.
14. Yes; explicit start avoids unintended background behavior and preserves domain intent. **Verified by operational safety rationale**.
15. Yes; use `ScheduleAuditAggregate` for durable scheduler history while keeping scheduler grain infrastructure-only. **Verified by architectural policy alignment**.
16. Yes; separate grains for concerns are valid when state ownership remains explicit and aggregate-backed for persistence. **Verified by design constraints**.

## Crash Matrix

| Crash Window | Expected State on Restart | Recovery Action | Survives? | Preconditions |
| --- | --- | --- | --- | --- |
| Before schedule registration request | No schedule | Caller retries registration or command path retries | Yes | Idempotent registration |
| After registration request, before reminder persisted | Uncertain schedule presence | Reconciliation ensures schedule exists | Yes | Reconciliation enabled |
| During reminder tick command dispatch | Active schedule | Next tick re-dispatches command | Yes | Idempotent command handler |
| After command side effect, before event append | Domain state not advanced | Tick retries command | Yes | Strong idempotency key |
| During cancellation | Terminal/disabled | Later tick detects terminal and unregisters | Yes | Terminal no-op logic |
| Full cluster outage | Schedules paused | Reminders resume after cluster recovery | Yes | Durable reminder provider |
| Duplicate reminders/ticks | Extra dispatch attempts | Handler deduplicates and no-ops safely | Yes | Idempotency enforcement |
| Metadata exists but schedule never started | No active reminder | No tick occurs until explicit start | Yes | Explicit start API/command |
| Scheduler grain loses volatile context | Audit required | Reconstruct from `ScheduleAuditAggregate` + active reminder reconciliation | Yes | Audit mode enabled |

## What Changed After Verification

- Reframed scope to aggregate-first scheduling capability.
- Kept reconciliation and idempotency enforcement as mandatory requirements.
- Moved saga-specific checkpoints/resume logic to phase 2 on top of phase 1 primitives.
- Added explicit durable audit path using `ScheduleAuditAggregate`.

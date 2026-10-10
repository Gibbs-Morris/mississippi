# Verification Status and Required Evidence

This specification is a draft proposal. Source inspection against main `c8da151e607bcc8f3b253519317a8e7418d26261` on 4 October 2026 refreshes repository facts; it does not validate the proposed scheduler or its crash guarantees.

## Source Findings

| Question | Evidence and result |
| --- | --- |
| Does the repository already use saga reminders? | Yes. `GenericAggregateGrain` implements `IRemindable`; `OrleansSagaReminderRegistry` registers and unregisters reminders. |
| Are reminder packages already referenced? | Yes. `DomainModeling.Runtime.csproj` references `Microsoft.Orleans.Reminders`; versions are centrally managed. |
| Is the generic scheduled-command API implemented? | No implementation of the proposed scheduling attributes, manager or audit aggregate was found in the inspected `src` tree. |
| Is a manual saga continue command merged? | No. PR #361 is separate unmerged work. |
| Can phase and last completed forward step establish rollback progress? | No. `ISagaState` has no remaining compensation cursor, and the compensation/failure reducers only set phase. |
| Is an in-memory last-tick token sufficient for delayed older deliveries? | No. It only recognizes the most recently recorded token; the earlier examples omit retention/generation/checkpoint behavior. |
| Do source references prove a configured persistent reminder store? | No. Actual host configuration and restart tests are required for the target deployment. |

See [learned.md](learned.md) for direct source links. The February design arguments remain proposals; they are not evidence that these APIs or runtime guarantees are implemented.

## Proposed Acceptance Requirements

- Persist active/disabled state, registration generation, command binding, policy, next due identity and retry/checkpoint progress regardless of optional audit mode.
- Reconcile proven absence separately from lookup/append timeouts and other unknown storage outcomes. An exception cannot prove that a write did not commit.
- Validate a request's schedule generation and authoritative aggregate checkpoint when the command executes. Single-grain serialization does not invalidate a stale plan already sent elsewhere.
- Use a stable tick identity across retries and a durable per-schedule replay strategy that handles older delayed deliveries, rather than one last-token equality check.
- Separate internal event deduplication from arbitrary external-effect outcomes. Require downstream deduplication/reconciliation or explicit intervention when the outcome is unknown.
- Stop/update must invalidate stale ticks. Unregistering a reminder cannot retract a command already dispatched or a callback already queued.
- Saga recovery must preserve direction and remaining compensation position, block incompatible workflow definitions, obey bounded policy, and meet #404's operator authorization/comment/audit requirements.
- Keep the existing saga reminder owner coherent with any future generic scheduler. This PR does not authorize a second automatic recovery owner.

## Crash Matrix to Implement and Test

Every row below is **unverified for the proposed scheduler**. The expected outcome is an acceptance requirement, not a passing test result.

| Boundary | Required decision and outcome | Evidence still required |
| --- | --- | --- |
| Start request before control state commits | Retry with the same operation identity; distinguish absence from uncertainty. | Fault injection before and after the durable control append. |
| Control state commits before reminder registration completes | Reconcile the confirmed active generation without inventing registration success. | Restart and provider-failure tests. |
| Reminder callback after stop/update | Reject obsolete generation at authoritative execution; do not start new obsolete work. | Queued callback and in-flight dispatch race tests. |
| Dispatch commits but its acknowledgement is lost | Retry/reconcile the same tick identity without a second transition. | Commit/acknowledgement fault injection and durable dedup tests. |
| External effect succeeds before completion is recorded | Preserve unknown outcome; use downstream deduplication/reconciliation or intervention. | Non-idempotent effect tests and policy tests. |
| Older tick arrives after a newer tick | Apply the defined ordering/missed-tick policy; never rely only on last-token equality. | Reordered and repeated delivery cases. |
| Scheduler restarts with audit disabled | Rebuild control state and reconcile the intended reminder. | Audit-off restart and provider integration tests. |
| Saga resumes after partial rollback | Preserve compensation direction and remaining cursor, or reject when evidence is absent. | Recorded refund/release-stock regression and lifecycle tests. |
| Full cluster/store outage | Report unavailable/unknown state; recover only when confirmed state and provider permit it. | Deployment-specific recovery tests. |

## Document Validation

Markdown lint, links and source mappings must be checked on the final PR revision. Build/test/cleanup evidence for the repository must be reported separately. Passing those checks validates the proposed documents and unchanged code baseline; it cannot prove an unimplemented scheduler works.

Mutation testing is optional under current repository policy. No mutation result or runtime scheduler coverage is claimed by this specification.

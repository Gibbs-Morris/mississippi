# Aggregate Scheduled Commands Spec

> Draft proposal only. Source facts were refreshed against main `c8da151e607bcc8f3b253519317a8e7418d26261` on 4 October 2026. Scheduling types shown here are proposed contracts. They are not implemented or validated by this PR. Recovery requirements remain tracked in [#404](https://github.com/Gibbs-Morris/mississippi/issues/404) and [#581](https://github.com/Gibbs-Morris/mississippi/issues/581).

The folder name `saga-reminder-auto-resume` preserves the original motivation. The proposal expanded to generic aggregate scheduling, with saga adoption as a later phase.

## Status

Draft design; implementation approval is outstanding. This PR updates eight proposal documents and adds no scheduling runtime, dependencies or public contracts.

## Goal

Design opt-in, explicitly started aggregate schedules using Orleans reminders. Define recovery and duplicate handling before implementation, then assess saga adoption against the existing runtime and the recovery requirements in #404 and #581.

## Existing behavior

Current main already has saga reminder registration and recovery handling in `GenericAggregateGrain` and `OrleansSagaReminderRegistry`. Generic scheduling attributes, a scheduling manager and scheduler control/audit aggregates are still proposed. See [learned.md](learned.md) for source links and [verification.md](verification.md) for unverified acceptance requirements.

The manual command in [#361](https://github.com/Gibbs-Morris/mississippi/pull/361) is separate and unmerged. It does not establish safe recovery direction or compensation progress for failed or compensating sagas.

## Proposed scope

- Register named command bindings without automatically starting schedules.
- Provide explicit start, update and stop operations.
- Persist operational control state in every audit mode, including generations, active state, policy and tick progress.
- Reject obsolete generations at the authoritative aggregate and persist duplicate decisions.
- Define retry bounds, unknown-effect handling and reminder/control-store reconciliation.
- Keep optional audit history separate from the control state needed for restart.
- Assess saga adoption only after durable recovery direction, compensation cursor and operator policy are agreed.

## Approval checkpoint

Implementation needs a separate reviewed decision because it changes aggregate scheduling contracts and runtime behavior. Updating this proposal does not approve those changes or close #404/#581.

## Documents

- Source facts: [learned.md](learned.md)
- Proposal: [rfc.md](rfc.md)
- Acceptance requirements and evidence gaps: [verification.md](verification.md)
- Future implementation plan: [implementation-plan.md](implementation-plan.md)
- Partial examples: [code-samples.md](code-samples.md)
- Partial interfaces: [grain-interfaces.md](grain-interfaces.md)
- Historical and current progress: [progress.md](progress.md)

## Out of scope

UI changes, other scheduler engines, scheduler implementation and claims of exactly-once arbitrary external effects.

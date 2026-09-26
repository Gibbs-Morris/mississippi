# Coordinator work record and worker contract

Copy the relevant fields into the target's existing tracker or a private run
record outside this package. This is a template, not active operational state.
Use one stable record shared across working copies; do not maintain divergent
branch copies. Store no credentials or private findings in public artifacts.

## Run

- Run identity, requested outcome, tracker link and complete acceptance IDs.
- Original coordinator identity and reporting destination.
- Resolved repository identity/root, instruction sources and input hashes.
- Baseline head/base and verified commands, client capabilities and limits.
- Decision revision and material assumptions/decisions with affected tasks.
- Active-worker limit and total unfinished-work limit with reasons.
- Shared resource owners, serialization rules and integration/stack owner.
- Tasks and dependency edges; intended PR boundaries and landing constraints.
- Last observed progress in UTC, live operation handles and next action.
- Current validation and outstanding review/CI work; delivery boundary.

## Task / assignment

- Stable task ID and authoritative attempt ID; one active owner.
- Outcome, acceptance IDs and expected observable result.
- Delegated-worker mode; no direct user contact or recursive coordination.
- Required instruction reads, constraints and non-goals.
- Allowed edits/actions; excluded shared contracts/resources and permissions.
- Starting commit, prerequisites and decision revision.
- Verified workspace root/branch and resource reservations.
- Discovered validation commands, evidence requirements and reporting endpoint.
- Escalation conditions, observed-progress timestamp and next action.
- State: planned, active, blocked, implemented, reviewed, integrated, published,
  or merged; result references, evidence and PR links as applicable.

## Worker return

- Task/attempt/owner, decision revision, actual root, starting base and result
  commit or recoverable patch reference; changed scope.
- Status, validation commands/outcomes/artifact paths and tested revisions.
- Deviations, every material concern, blocker and decision needed.
- For clarification: uncertainty, evidence checked, affected work and options.
- For a failed attempt: live process/operation status and recoverable references.

## Coordinator acceptance and resumption

Verify each return's identity, current attempt, scope, prerequisites, ancestry
and decision revision before integration. Quarantine superseded or late returns;
never accept them silently over a replacement. Worker completion is task input,
not proof of delivered completion. Propagate material decisions to every
affected worker; revoke stale assignments and invalidate dependent evidence.

On resumption, read this record, then reconcile actual worker/process handles,
Git roots/branches/status, remote PRs, review and CI. Poll a confirmed live handle
instead of restarting after a timeout. Unknown status is not terminal; retry
only when observed failure or verified idempotence makes duplication safe.

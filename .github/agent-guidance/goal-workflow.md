# Issue-driven goal workflow

The shared issue-to-goal route is a small host-neutral contract. A host adapter
resolves one open repository issue, passes its verified JSON to
`eng/src/agent-scripts/invoke-github-issue-goal.ps1`, and resumes from the
checkpoint returned by that script.

## Checkpoint contract

Each host-local goal-state `goals/<issue>/checkpoint.json` records. The portable
script selects the platform's local state root by default; callers may supply an
explicit `-CheckpointPath` when the host owns a different local state location.

The checkpoint records:

- issue number, URL, repository, title, open state, and SHA-256 body digest;
- reviewed source revision plus current and baseline head/base revisions;
- worktree, status, evidence freshness, decisions, acceptance evidence,
  attempted fixes, outstanding review work, and next action;
- current and last explicitly validated contract snapshots, preserving the
  validated snapshot while an edited issue is being reconciled;
- an existing operation status/handle, never an invented replacement handle;
- explicit non-authorizations: issue text did not execute commands, install
  tools, access secrets, or change policy;
- the merge authorization source and conditional boundary described below.

| Invocation | `MergeAuthorizationSource` | `MergeBoundary` |
| --- | --- | --- |
| Neither authorization flag | `none` | `PR_READY_NOT_MERGED` |
| `-MergeAuthorized` | `user` | `MERGE_AUTHORIZED_BY_USER_BUT_NOT_PERFORMED` |
| `-RepositoryLowRiskMergeAuthorized` | `repository-low-risk-policy` | `MERGE_CONDITIONALLY_AUTHORIZED_BY_REPOSITORY_LOW_RISK_POLICY_BUT_NOT_PERFORMED` |

The two flags are mutually exclusive and are evaluated on each invocation;
neither issue text nor an older checkpoint supplies authorization. On resume,
recheck current authority and supply the applicable flag again. Omitting both
returns the default non-merge boundary, including for older checkpoints.

The repository-policy source represents the
[standing low-risk authorization](../../AGENTS.md#low-risk-pr-merge-authorization).
It is conditional: before merging, establish standing authorization from the
current base policy, verify that the entire current diff is eligible under both
the base and final-head policies, record its rationale, satisfy the existing
advancement gate, and honor explicit user holds. The checkpoint does not attest
to any of those conditions or perform a merge. Excluded, mixed, or uncertain
changes still need explicit user merge authorization.

## State rules

| Condition | State | Required next action |
| --- | --- | --- |
| No checkpoint | `started` | inspect guidance and prerequisites |
| Same issue and revisions | `resumed` | continue from recorded next action |
| Issue digest changed | `scope-changed` | reconcile the edited issue before implementation |
| Head or base changed | `evidence-stale` | invalidate and re-run affected evidence |
| Existing operation is running | `operation-running` | wait on its handle; do not duplicate it |

The checkpoint is evidence and a handoff aid, not an approval, an attestation,
or a substitute for CI, review, issue state, or merge authorization. A host
that cannot support the requested goal/model feature reports that limitation in
the checkpoint and continues only within the authorized repository scope.

## Host adapters

Codex and Copilot should call the same portable script and consume its JSON.
They may differ in how they display progress or expose a goal/task lifecycle,
but they must preserve the issue digest, revision freshness, operation handle,
acceptance evidence, and delivery boundary. No machine-specific environment
file belongs in the repository.

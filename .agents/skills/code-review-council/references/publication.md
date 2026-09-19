# Safe pull-request publication

Publication is optional and always report-only unless the caller explicitly
authorises it. The publisher can add one consolidated top-level comment; it
cannot approve, request changes, resolve threads, merge, close, or change PR
permissions.

## Preconditions

Before a write, the publisher must have:

- a final result tied to a current scope ID;
- an explicit caller authorisation naming the target PR and intended comment;
- expected base and head SHAs from the reviewed snapshot;
- valid diff anchors for every inline claim, or a top-level report when no
  inline anchor can be proven; and
- an idempotency key derived from the snapshot ID, result status, and finding
  fingerprints.

The GitHub provider re-reads the live PR immediately before writing and refuses
to continue if the PR is closed, the base/head changed, or the expected diff
anchors are no longer present. A stale response is an incomplete publication,
not a successful retry signal.

## Idempotency

The comment contains a machine-readable marker:

```text
<!-- code-review-council:v1:<idempotency-key> -->
```

The publisher searches existing comments for that exact marker before posting.
If it exists, the result is `already-published`; it does not create a second
comment. The local mock provider uses the same ledger behavior so retries can
be tested offline.

## Provider behavior

- `mock` validates the final result and updates only a caller-selected local
  ledger. It is the safe default for tests and demonstrations.
- `github` uses the pre-existing `gh` CLI, validates the repository and PR
  identifiers, rechecks the live PR, and requires `--execute` immediately
  before the comment POST. It does not accept shell fragments or execute
  reviewed repository content.
- Any missing CLI, authentication, permission, live PR data, or anchor causes
  an explicit blocked publication result. It must not be described as posted.

The implementing agent or the repository’s pull-request-feedback workflow owns
fixes and review-thread disposition after publication. A council comment is
evidence, not approval.

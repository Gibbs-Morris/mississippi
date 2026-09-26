# Safe pull-request publication

Publication is optional and always report-only unless the caller explicitly
authorises it. The publisher can add one consolidated top-level comment; it
cannot approve, request changes, resolve threads, merge, close, or change PR
permissions.

## Preconditions

Before a write, the publisher must have:

- a final result tied to a current scope ID;
- an explicit caller authorisation naming the target PR and intended comment;
- a hashed `pull-request` scope snapshot bound to the requested repository,
  PR number, base SHA, and head SHA, with the expected base and head supplied;
- every finding path present in the live PR file list and every finding line
  present in the live PR diff response whose file headers match that file list;
- distinct resolved paths for the review input, optional Markdown input,
  optional ledger, and optional output, so a status write cannot replace its
  evidence or idempotency state; and
- an idempotency key derived from the snapshot ID, result status, and finding
  fingerprints.

The GitHub provider reads the live file list and requests the live PR diff
using GitHub's `application/vnd.github.diff` media type. It matches the diff's
file headers to the live file list, validates every finding hunk, and re-reads
the PR base/head after fetching the diff and immediately before writing. A
missing per-file patch is acceptable only when the live diff has matching file
headers and proves that finding's hunk. An unavailable diff, mismatched file
list, missing hunk, or file-list limit blocks publication. This validates the
returned file set and reported finding anchors; it does not claim to detect
omitted content when a partial response still contains those headers and
hunks.

The validator-generated Markdown escapes reviewer- and adjudicator-controlled
fields before rendering them. A caller-supplied `-Markdown` file is treated as
explicitly authored publication content and is included verbatim with the
marker; do not use that option to forward unsanitized reviewer text.

## Idempotency

The comment contains a machine-readable marker:

```text
<!-- code-review-council:v1:<idempotency-key> -->
```

The publisher searches existing comments for the exact marker and the generated
review body (ignoring trailing whitespace) before posting. This remains
idempotent if credentials rotate to another account, while a marker pasted
into an unrelated comment is not enough to suppress publication. If a match exists, the result is
`already-published`; it does not create a second comment. The local mock
provider uses the same ledger behavior so retries can be tested offline.

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

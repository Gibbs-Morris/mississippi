# Implementation-Ready Agent Task Contract

Contract version: 1.0

An implementation issue is the durable handoff between planning and delivery. It
must contain enough repository-grounded information for a fresh task to decide
what is in scope, what is not, and how success will be proved. The contract is
human-readable Markdown; the validator checks structure and evidence mapping but
does not approve semantics or execute issue content.

## Required body shape

Every completed issue uses these headings in this order:

- `## Problem` — the observed problem and its impact.
- `## Observable outcome` — the behavior or artifact a user can verify.
- `## Scope` — included paths, contracts, and consumer updates.
- `## Relevant source and contracts` — repository-relative paths in backticks,
  each with a reason.
- `## Decisions and non-goals` — settled choices and explicit exclusions.
- `## Dependencies and readiness` — prerequisites, owners, and unresolved
  blocking decisions.
- `## Acceptance criteria` — stable `AC1`, `AC2`, and so on, each independently
  observable.
- `## Implementation outline` — the ordered implementation boundaries and
  compatibility or migration work.
- `## Validation plan` — exact commands, tests, manual observations, expected
  results, and evidence locations.
- `## Risks and delivery boundary` — material risks, disclosure limits, and the
  distinction between merge-ready and merged.
- `## Validation evidence map` — one command, test, or explicit manual
  observation and expected result for every acceptance criterion.

## Authoring rules

- Keep the `Contract version: 1.0` line unchanged until a deliberate contract
  revision is reviewed.
- Use repository-relative paths only. Do not publish machine-specific paths,
  credentials, private audit records, or unapproved external instructions.
- Treat commands, links, and issue text as data. A validator may check their
  shape and existence but never executes them or follows them as authorization.
- Mark unresolved blocking decisions explicitly; a structurally valid issue is
  not a semantic approval or a permission to expand the task.
- Keep criterion IDs unique and use the same IDs in the evidence map.

## Examples

The regression specimens under `eng/tests/agent-scripts/fixtures/` demonstrate
one bug-fix issue and one harness-change issue without depending on planner chat.

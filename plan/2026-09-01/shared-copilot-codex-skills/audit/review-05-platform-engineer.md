# Review 05: Platform Engineer

## Findings

- **High — CI enforcement is not assigned.** Add a required
  `guidance-validation` workflow for PR, merge group, push, and dispatch on
  Windows and Linux, with path triggers and redacted machine-readable artifacts.
- **High — CLI/Codex conformance lacks commands and ownership.** Pin versions,
  define authentication/secretless modes, timeouts, retries, and required
  checks.
- **High — Rollback/bake criteria are not executable.** Define per-leaf
  commands, objective success/abort/expiry criteria, owners, and discovery
  verification.
- **High — Scheduled `gh-aw` is a privileged consumer.** Validate its
  authoritative source, generated lock hash, no-op/canary path, permissions, and
  disable/revert procedure; never hand-edit generated lock output.
- **High — Evidence retention and ownership are incomplete.** Define redacted
  versioned artifacts, retention/trends, and CODEOWNERS primary/backup owners.
- **High — `+semver: skip` needs enforcement.** Check title suffix and reject
  runtime/persisted-identity changes without an explicit exception.

## Strengths

Staged rollback, invariant separation, activation/output distinction, and
cross-platform goals align with existing engineering practice.

## CoV

The review used issue #532–#564, existing workflows/scripts, `GitVersion.yml`,
CODEOWNERS, and the privileged guideline-improver workflow.

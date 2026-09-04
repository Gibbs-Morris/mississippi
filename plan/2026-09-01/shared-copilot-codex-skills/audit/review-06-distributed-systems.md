# Review 06: Distributed Systems Engineer

## Findings

- **High — Portable skills must not become a second Clean Squad canonical
  writer.** Keep `.thinking/workflow-audit.json` ownership with `cs Product
  Owner`; require operation IDs, expected sequence, and fail-closed retries.
- **High — Parallel leaves need exclusive target ownership.** Use matrix owners
  as enforceable locks, disjoint scopes, serialized shared-file merges, and
  post-merge validation.
- **High — External side effects need idempotency.** Use stable operation IDs,
  intent digests, read-before-create/update, bounded retries, and ambiguous
  outcome stops for issues, PRs, comments, and artifacts.
- **High — Cold/warm/restart/rollback semantics are missing.** Record a guidance
  fingerprint and test active sessions that began before cutover.
- **High — Preserve Orleans non-guarantees.** Keep turn scope, at-least-once
  recovery, eventual projections, no global ordering, and no exactly-once claims
  in always-on/scoped guidance.
- **High — Avoid broad Orleans scope.** Validate grain and non-grain fixtures so
  nested guidance does not impose POCO-grain rules on unrelated C# files.

## Strengths

Dependency ordering, pilot gating, activation/output separation, and rollback
intent are sound.

## CoV

Evidence includes the Clean Squad workflow ledger, scratchpad ownership rules,
Orleans instructions, and Mississippi distributed-system documentation.

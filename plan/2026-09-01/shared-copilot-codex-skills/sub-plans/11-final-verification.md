# Sub-Plan 11: Final verification and migration metrics

## Context

- Master plan: `../PLAN.md`
- Issue: #564
- This is sub-plan 11 of 24.

## Dependencies

- Depends on: 10b and every earlier sub-plan.

## Objective

Prove the completed programme is portable, secure, maintainable, reversible,
and objectively ready to close.

## Scope

- full validation scenario corpus and all surface smoke evidence
- final matrix/manifest, support contract, maintenance model, and metrics
- residual-gap ledger and closure report

## Deployability

- Feature gate: verification only; no new behavior.
- Safe to deploy: closure is blocked unless all required evidence is complete.

## Implementation breakdown

1. Run full CLI/Codex conformance and native Copilot app/VS Code smoke tests;
   execute the defined Local/cloud/code-review/Visual Studio/JetBrains checks.
2. Validate case-sensitive discovery, parser/schema, references, capabilities,
   secret/redaction, generated automation, ownership, and rollback.
3. Compare both historical baselines and publish units/denominators, context,
   duplicate, collision, and maintenance metrics.
4. Verify every disposition, retired reference, exception, residual finding,
   and owner; publish final support and maintenance documentation.
5. Close the epic only if all hard gates pass; otherwise file/defer each gap.

## Testing strategy

Use clean Windows and Linux checkouts, cold/warm/restart sessions, pinned
fixtures, isolated live samples, full reverse rollback, and final artifact
digest verification.

## Acceptance criteria

- [ ] All programme/workstream acceptance criteria have objective evidence.
- [ ] No unexplained duplicate, unsupported surface, stale reference, or
  deferred risk remains.
- [ ] Every hard check is `PASS`; missing evidence is `BLOCKED`.
- [ ] Baseline, support, security, rollback, ownership, and maintenance
  reports are published.
- [ ] Epic closure does not rely on unchecked task-list assertions.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/11-final-verification`
- Title: `Verify shared Copilot and Codex skills migration +semver: skip`
- Base: `main`

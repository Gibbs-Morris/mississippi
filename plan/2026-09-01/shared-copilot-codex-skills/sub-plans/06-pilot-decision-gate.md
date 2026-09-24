# Sub-Plan 06: Pilot evidence decision gate

## Context

- Master plan: `../PLAN.md`
- Issue: #545
- This is sub-plan 06 of 24.

## Dependencies

- Depends on: 05
- Plan approval/PR 1 is required before execution.

## Objective

Use pilot evidence to approve, revise, or stop each later migration wave.

## Scope

- pilot reports and redacted harness artifacts
- updated migration matrix and dependency state
- amendments to the authoring standard, harness, catalogue, or sub-plans

## Deployability

- Feature gate: decision-only; no new user-visible behavior.
- Safe to deploy: later migrations remain locked until an explicit `go`.

## Implementation breakdown

1. Compare CLI/Codex activation, output, portability, collisions, context,
   review effort, maintenance, and rollback with the contract.
2. Check hard gates: matrix completeness, stale roots, security, references,
   deterministic fixtures, and evidence redaction.
3. Record `go`, `revise`, or `stop` per workstream with evidence digest.
4. On `revise`, invalidate affected sub-plans; on `stop`, defer/close without
   manual bypass.
5. Update dependency metadata before unlocking any wave.

## Testing strategy

Re-run the pilot evidence from a clean snapshot, verify threshold calculations,
and test that dependency resolution blocks revise/stop states.

## Acceptance criteria

- [ ] Decision covers every later workstream.
- [ ] Successful and failed scenarios are both recorded.
- [ ] Thresholds and residual uncertainty are explicit.
- [ ] `dependencies.json` reflects the gate state.
- [ ] No later leaf starts without an approved `go`.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/06-pilot-decision-gate`
- Title: `Approve shared skill migration waves from pilot evidence +semver: skip`
- Base: `main`

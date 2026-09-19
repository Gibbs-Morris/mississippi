# Sub-Plan 07b: Improve-legacy-tests skill

## Context

- Master plan: `../PLAN.md`
- Issue: #547
- This is sub-plan 07b of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Guide tests-only legacy improvement through characterization, meaningful
assertions, deterministic design, and regression verification.

## Scope

- `.agents/skills/improve-legacy-tests/SKILL.md`
- test-level/naming and deterministic-testing references
- legacy test instruction and QA/test-engineer source rows

## Deployability

- Feature gate: introduce/redirect/optional retire; no application behavior.
- Safe to deploy: production changes remain explicitly justified and separate.

## Implementation breakdown

1. Characterize current behavior and identify source-to-test gaps.
2. Design behavior-oriented L0/L1 tests with deterministic time/randomness.
3. Keep work under `tests/` unless explicit approval authorizes production code.
4. Run regression, coverage, and mutation checks at the applicable level.
5. Handle flaky/nondeterministic tests with a stop and evidence path.

## Testing strategy

Use representative assertion-depth, test-level, flaky, and production-change
fixtures; verify no mutation-only behavior changes are recommended.

## Acceptance criteria

- [ ] Tests demonstrate meaningful behavior, not line coverage alone.
- [ ] Production edits are separately authorized and reviewable.
- [ ] Test naming/levels and deterministic rules are preserved.
- [ ] Primary runtimes and structural checks pass.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07b-improve-legacy-tests`
- Title: `Migrate improve-legacy-tests workflow to shared skill +semver: skip`
- Base: `main`

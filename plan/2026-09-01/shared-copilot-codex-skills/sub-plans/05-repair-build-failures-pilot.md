# Sub-Plan 05: Build-failure remediation pilot

## Context

- Master plan: `../PLAN.md`
- Issue: #544
- This is sub-plan 05 of 24.

## Dependencies

- Depends on: 04
- #540 must have made the selected skill root writable/discoverable.

## Objective

Prove the full strangler strategy with a safe `repair-build-failures` skill.

## Scope

- `.agents/skills/repair-build-failures/SKILL.md`
- focused references for failure taxonomy and evidence output
- pilot scenario fixtures and matrix rows
- introduce, redirect/reduce, and rollback PR slices

## Deployability

- Feature gate: introduce first with old build/remediation guidance active.
- Safe to deploy: redirect only after CLI/Codex hard checks and pilot evidence;
  retirement remains optional.

## Implementation breakdown

1. Define restore, environment, compilation, test, formatting, analyzer, and
   tool failures and earliest-actionable-error triage.
2. Reference canonical scripts and preserve minimal-edit/five-attempt/defer
   behavior without suppressions or success-shaped fallbacks.
3. Require exact commands, results, skips, uncertainty, and rollback evidence.
4. Run explicit, implicit, paraphrased, negative, missing-tool, adversarial,
   and repeatability scenarios in isolated fixtures.
5. Redirect only verified consumers and record the old-to-new mapping.

## Testing strategy

Use scripted failure fixtures and both primary runtimes; verify 0% false
positive activation on unrelated repair/review/research prompts and honest
`BLOCKED` output for missing tools.

## Acceptance criteria

- [ ] Skill passes structural and primary-runtime conformance.
- [ ] No suppression or broadening is recommended to make a gate green.
- [ ] Successful output contains exact verification evidence.
- [ ] Redirect has a tested reverse path and no mixed canonical source.
- [ ] Pilot results are ready for #545.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/05-repair-build-failures-pilot`
- Title: `Pilot repair-build-failures skill +semver: skip`
- Base: `main`

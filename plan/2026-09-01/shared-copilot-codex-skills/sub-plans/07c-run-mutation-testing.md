# Sub-Plan 07c: Run-mutation-testing skill

## Context

- Master plan: `../PLAN.md`
- Issue: #548
- This is sub-plan 07c of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Run bounded canonical Stryker workflows and turn surviving mutants into
behavior-oriented test improvements without hiding failures.

## Scope

- `.agents/skills/run-mutation-testing/SKILL.md`
- mutation references and survivor-reporting examples
- mutation instruction/test-agent source rows

## Deployability

- Feature gate: introduce/redirect/optional retire; old mutation guidance stays
  active through validation.
- Safe to deploy: no production behavior change.

## Implementation breakdown

1. Define scope, `dotnet tool restore`, clean-build prerequisites, and canonical
   mutation commands.
2. Bound runs by target/project and preserve Stryker configuration ownership.
3. Classify survivors, report score changes, exclusions, and unresolved items.
4. Require full completion when a mutation gate is invoked; do not cancel or
   silently exclude survivors.
5. Test collision with `verify-change` and legacy-test improvement.

## Testing strategy

Use replayable reports for killed/surviving/excluded mutants, missing-prereq
`BLOCKED` cases, and no-behavior-change constraints.

## Acceptance criteria

- [ ] Canonical commands and prerequisites are exact.
- [ ] Mutation is not treated as a substitute for behavior tests.
- [ ] No unjustified exclusions or hidden survivors.
- [ ] Scores and residual uncertainty are reported.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07c-run-mutation-testing`
- Title: `Migrate mutation-testing workflow to shared skill +semver: skip`
- Base: `main`

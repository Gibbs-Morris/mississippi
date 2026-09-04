# Sub-Plan 07a: Verify-change skill

## Context

- Master plan: `../PLAN.md`
- Issue: #546
- This is sub-plan 07a of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Provide a risk-based workflow that selects the smallest canonical build, test,
formatting, analysis, and documentation checks and reports truthful evidence.

## Scope

- `.agents/skills/verify-change/SKILL.md`
- references for path/risk-to-check mapping
- source instruction/agent rows and evaluation fixtures

## Deployability

- Feature gate: introduce/redirect/optional retire; old verification guidance
  remains until conformance passes.
- Safe to deploy: guidance-only and reversible.

## Implementation breakdown

1. Derive changed-path and risk inputs.
2. Select canonical targeted checks before broader checks.
3. Require exact command/result/skip/residual-uncertainty output.
4. Keep mandatory completion gates outside the skill and test neighboring
   `repair-build-failures` and `prepare-pull-request` prompts.

## Testing strategy

Exercise code, tests, docs, scripts, workflows, and runtime-identity paths,
including missing tools, unrelated prompts, and false success claims.

## Acceptance criteria

- [ ] No success without evidence.
- [ ] Expensive unrelated checks are justified, not automatic.
- [ ] Required gates remain enforceable without activation.
- [ ] Primary runtimes and structural validator pass.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07a-verify-change`
- Title: `Migrate verify-change workflow to shared skill +semver: skip`
- Base: `main`

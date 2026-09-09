# Sub-Plan 07e: Review-change skill

## Context

- Master plan: `../PLAN.md`
- Issue: #550
- This is sub-plan 07e of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Provide a shared evidence-first review procedure while preserving specialist
lenses and isolated agents where they add value.

## Scope

- `.agents/skills/review-change/SKILL.md`
- reviewer evidence/severity/confidence references
- CoV/VFE reviewer source rows and lens fixtures

## Deployability

- Feature gate: introduce/redirect/optional retire; existing reviewers remain
  available until conformance passes.
- Safe to deploy: produces findings only and does not merge or resolve threads.

## Implementation breakdown

1. Define diff scope, evidence requirements, severity/confidence, and no-finding
   output.
2. Support explicit lens selection without loading every specialist reference.
3. Deduplicate findings and distinguish must-fix defects from suggestions.
4. Preserve agent isolation, tools, and orchestration boundaries; prohibit
   governed phase bypass and unauthorized thread actions.

## Testing strategy

Use positive/negative diff fixtures, overlapping lens prompts, duplicate
findings, no-findings, outdated comments, and unauthorized publication cases.

## Acceptance criteria

- [ ] Findings have file/line evidence and confidence.
- [ ] Lens selection is explicit and collision-tested.
- [ ] No-finding and must-fix outputs are deterministic.
- [ ] Clean Squad governance and publication boundaries remain intact.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07e-review-change`
- Title: `Migrate review-change workflow to shared skill +semver: skip`
- Base: `main`

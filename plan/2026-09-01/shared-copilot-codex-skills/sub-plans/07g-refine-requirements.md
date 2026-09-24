# Sub-Plan 07g: Refine-requirements skill

## Context

- Master plan: `../PLAN.md`
- Issue: #552
- This is sub-plan 07g of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Turn incomplete requests into evidence-backed, testable requirements without
silently inventing scope or publishing external changes.

## Scope

- `.agents/skills/refine-requirements/SKILL.md`
- requirements/acceptance/decision-log reference
- requirements-agent source rows

## Deployability

- Feature gate: introduce/redirect/optional retire; no issue is edited
  automatically.
- Safe to deploy: output is a proposed issue-ready artifact only.

## Implementation breakdown

1. Capture inputs, user outcome, assumptions, open questions, non-goals, and
   constraints.
2. Ask focused ranked questions and distinguish unknowns from requirements.
3. Write observable acceptance criteria and a decision log with evidence.
4. Produce issue-ready Markdown without external publication unless separately
   authorized.

## Testing strategy

Use incomplete, contradictory, ambiguous, already-specified, and
publication-request prompts; verify clarification and stop behavior.

## Acceptance criteria

- [ ] Criteria are observable and testable.
- [ ] Unknowns are not converted into facts.
- [ ] External issue writes require exact explicit authorization.
- [ ] Clean Squad governed workflows cannot be bypassed.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07g-refine-requirements`
- Title: `Migrate refine-requirements workflow to shared skill +semver: skip`
- Base: `main`

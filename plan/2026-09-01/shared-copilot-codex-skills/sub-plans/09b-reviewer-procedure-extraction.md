# Sub-Plan 09b: Reviewer procedure extraction

## Context

- Master plan: `../PLAN.md`
- Issue: #558
- This is sub-plan 09b of 24.

## Dependencies

- Depends on: 09a, 07e; PR 1 and a #545 `go` are required.

## Objective

Extract one canonical evidence/severity/confidence/deduplication review
procedure while retaining useful specialist lenses and isolation.

## Scope

- approved `review-change` skill/references
- CoV/VFE reviewer mechanics
- reviewer handoffs, lens selection, and synthesis contracts

## Deployability

- Feature gate: introduce shared mechanics before thinning shells.
- Safe to deploy: specialist agents remain available until redirect evidence.

## Implementation breakdown

1. Compare reviewer agents and remove only duplicated mechanics.
2. Define explicit lens selection, parallel reviewer output, deduplication, and
   synthesis without loading every specialist domain by default.
3. Preserve tool/context isolation and governed orchestration.
4. Redirect and validate retained shells; retire only with consumer closure.

## Testing strategy

Use overlapping lenses, duplicate findings, no-finding, severity/confidence,
outdated review, handoff, and unauthorized action fixtures.

## Acceptance criteria

- [ ] Shared procedure has one canonical source.
- [ ] Specialist knowledge and isolation remain available.
- [ ] Parallel results synthesize without duplicate findings.
- [ ] Every shell change has rollback and discovery evidence.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/09b-reviewer-procedure-extraction`
- Title: `Extract shared reviewer procedures +semver: skip`
- Base: `main`

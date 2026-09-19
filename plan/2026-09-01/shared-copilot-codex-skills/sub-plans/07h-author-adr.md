# Sub-Plan 07h: Author-ADR skill

## Context

- Master plan: `../PLAN.md`
- Issue: #553
- This is sub-plan 07h of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Create evidence-backed MADR decisions with correct numbering and preserved
history.

## Scope

- `.agents/skills/author-adr/SKILL.md`
- ADR instructions and architecture-principles references
- ADR keeper/solution-architect source rows

## Deployability

- Feature gate: introduce/redirect/optional retire; no ADR is published without
  the normal review path.
- Safe to deploy: output is a draft decision record.

## Implementation breakdown

1. Decide when an ADR is warranted versus ordinary documentation.
2. Inspect existing numbering/history and use the repository MADR convention.
3. Record context, alternatives, decision, consequences, evidence, and
   superseding/amending links.
4. Preserve Clean Squad ownership and Docusaurus placement/review rules.

## Testing strategy

Use new, amend, supersede, duplicate-number, insufficient-evidence, and
non-ADR prompts; validate links and deterministic path selection.

## Acceptance criteria

- [ ] Output records a decision, not merely a design.
- [ ] Alternatives and consequences are substantive.
- [ ] Existing ADR history and links remain intact.
- [ ] Publication and governed phase transitions are not implicit.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07h-author-adr`
- Title: `Migrate author-ADR workflow to shared skill +semver: skip`
- Base: `main`

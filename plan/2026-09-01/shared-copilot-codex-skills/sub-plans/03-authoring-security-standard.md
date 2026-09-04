# Sub-Plan 03: Portable authoring and security standard

## Context

- Master plan: `../PLAN.md`
- Issue: #542
- This is sub-plan 03 of 24.

## Dependencies

- Depends on: 01, 02
- Plan approval/PR 1 is required before execution.

## Objective

Define the portable, outcome-based, secure skill contract used by every later
migration.

## Scope

- `.github/instructions/agent-skills.instructions.md`
- `.github/agent-guidance/skill-authoring-standard.md`
- Definition of Ready/Done checklists
- portable metadata, capability, provenance, version, and retirement schemas

## Deployability

- Feature gate: no existing workflow is redirected; standard only.
- Safe to deploy: it constrains future content and cannot remove current
  guidance.

## Implementation breakdown

1. Define when to split/combine skills and how descriptions avoid collisions.
2. Specify lowercase IDs, portable frontmatter, owner/version/supersedes
   metadata, progressive disclosure, and reference resolution.
3. Define default-deny paths/commands/network/GitHub/secret capabilities.
4. Require trust hierarchy, redaction, draft-only publication, provenance,
   pinned dependencies, and safe executable rules.
5. Define lightweight/full evaluation tiers, context limits, bake, rollback,
   and retirement criteria.

## Testing strategy

Validate standard examples with valid/invalid frontmatter, duplicate IDs,
untrusted-input, capability-denial, publication-approval, and rollback
fixtures. Confirm no mandatory invariant is placed only in a skill.

## Acceptance criteria

- [ ] DoR/DoD are reusable by every migration leaf.
- [ ] Portable core excludes host-only or experimental fields unless justified.
- [ ] Security defaults are deny-by-default and fail closed.
- [ ] Context/collision and rename/deprecation rules are measurable.
- [ ] All later sub-plans reference this standard.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/03-authoring-security-standard`
- Title: `Define portable skill authoring and security standard +semver: skip`
- Base: `main`

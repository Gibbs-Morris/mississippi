# Sub-Plan 09d: Agent shell rationalization

## Context

- Master plan: `../PLAN.md`
- Issue: #560
- This is sub-plan 09d of 24.

## Dependencies

- Depends on: 08b, 09b, 09c; all approved workflow skill leaves must be complete.

## Objective

Thin, merge, or retire custom-agent shells only after their consumers and
unique host value are proven.

## Scope

- `.github/agents/**/*.agent.md`
- Clean Squad workflow/roster and handoffs
- agent docs, picker metadata, CODEOWNERS, matrix, and discovery checks

## Deployability

- Feature gate: execute one bounded family at a time; retain rollback aliases
  or mappings during the compatibility window.
- Safe to deploy: every shell change is independently discoverable, reversible,
  and validated before the next family.

## Implementation breakdown

1. Apply #557 dispositions and redirect shared mechanics to approved skills.
2. Preserve persona, isolated context, delegation, allowlists, and tool
   boundaries in retained shells.
3. Update roster, handoffs, docs, picker metadata, and retired-name mappings.
4. Validate no stale references, cycles, duplicate routes, or governance bypass.
5. Retire only after bake/rollback and inbound-reference closure.

## Testing strategy

Run agent graph/frontmatter/host discovery tests, retained picker/handoff
smoke tests, retired-name mapping tests, and forward/reverse rollback.

## Acceptance criteria

- [ ] Every retained agent has a demonstrated shell purpose.
- [ ] Retired names have no unresolved references.
- [ ] CLI/app/VS Code discover the intended final agent set.
- [ ] Codex uses shared skills without requiring agent definitions.
- [ ] Clean Squad roster and canonical ownership remain valid.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/09d-agent-shell-rationalization`
- Title: `Rationalize custom-agent shells +semver: skip`
- Base: `main`

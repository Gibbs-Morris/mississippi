# Sub-Plan 08a: Documentation skill boundaries

## Context

- Master plan: `../PLAN.md`
- Issue: #555
- This is sub-plan 08a of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Measure the smallest reliable documentation skill taxonomy instead of copying
the current page-type filenames.

## Scope

- all `documentation-*.instructions.md`, page-focus, feature-docs,
  Markdown, technical-writer, and doc-reviewer sources
- candidate routing skill(s), references, and evaluation fixtures
- documentation migration matrix rows

## Deployability

- Feature gate: evaluation only; current documentation guidance remains active.
- Safe to deploy: no page rules are removed.

## Implementation breakdown

1. Map concept, how-to, tutorial, reference, troubleshooting, operations,
   migration, release-note, and getting-started outcomes and invariants.
2. Prototype one routing skill, a small outcome set, and retained path-specific
   routing.
3. Measure activation, collision, loading, and output quality.
4. Decide which details remain deterministic scoped guidance versus references
   and record rejected alternatives.

## Testing strategy

Use representative page-type fixtures, cross-type collisions, incomplete
requests, unrelated requests, structure/link/render checks, and no-silent-loss
comparisons.

## Acceptance criteria

- [ ] Decision is evidence-based, not filename-based.
- [ ] No page-type rule is silently lost.
- [ ] The approved taxonomy avoids loading every page rule by default.
- [ ] Mandatory structure remains deterministic where needed.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/08a-documentation-boundaries`
- Title: `Determine documentation skill boundaries +semver: skip`
- Base: `main`

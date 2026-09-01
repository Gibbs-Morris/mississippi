# Sub-Plan 08b: Approved documentation migration

## Context

- Master plan: `../PLAN.md`
- Issue: #556
- This is sub-plan 08b of 24.

## Dependencies

- Depends on: 08a; PR 1 and a #545 `go` are required.

## Objective

Implement only the documentation skill boundaries approved by #555 and retain
deterministic page/path rules.

## Scope

- approved `.agents/skills/` documentation skill directories and references
- Docusaurus documentation sources and page-type adapters
- technical-writer and doc-reviewer agent shells
- documentation validation fixtures and migration mappings

## Deployability

- Feature gate: introduce before redirect; old documentation guidance remains
  until every approved workflow passes.
- Safe to deploy: page structure and rendering are preserved; retirement is
  optional after rollback evidence.

## Implementation breakdown

1. Create approved skills with progressive-disclosure references, not one skill
   per current instruction filename.
2. Preserve path-specific structure, Markdown, rendering, and link checks.
3. Thin writer/reviewer agents to persona, context, delegation, and tools.
4. Run introduce/redirect/optional-retire stacks with generated catalogue and
   old-to-new mappings.

## Testing strategy

Run CLI/Codex activation/output checks, native Copilot smoke tests, Docusaurus
Markdown/link/structure/render checks, and rollback/reference closure.

## Acceptance criteria

- [ ] Every approved workflow passes cross-agent tests.
- [ ] Representative pages render and link correctly.
- [ ] No documentation type or mandatory path rule is lost.
- [ ] Retained agents have justified shell concerns only.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/08b-documentation-migration`
- Title: `Migrate approved documentation workflows +semver: skip`
- Base: `main`

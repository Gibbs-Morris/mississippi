# Sub-Plan 07i: Create-C4-diagram skill

## Context

- Master plan: `../PLAN.md`
- Issue: #554
- This is sub-plan 07i of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Create evidence-backed C4 diagrams at the level requested, using valid Mermaid
and repository Markdown conventions.

## Scope

- `.agents/skills/create-c4-diagram/SKILL.md`
- C4/Mermaid/Markdown references
- C4 diagrammer source rows and rendering fixtures

## Deployability

- Feature gate: introduce/redirect/optional retire; diagrams remain drafts until
  normal documentation review.
- Safe to deploy: no runtime behavior and no unverified relationship presented
  as fact.

## Implementation breakdown

1. Select context/container/component level from the user question.
2. Gather evidence for every component and relationship; label proposed
   elements separately.
3. Emit repository-supported Mermaid/Markdown at the correct location.
4. Validate syntax, rendering readability, links, and documentation ownership.

## Testing strategy

Use each C4 level, inferred/proven relationship, malformed Mermaid, unreadable
diagram, wrong output path, and unrelated architecture prompt.

## Acceptance criteria

- [ ] Every relationship maps to evidence or is labelled proposed.
- [ ] C4 level matches the question.
- [ ] Mermaid renders and links resolve.
- [ ] Governed documentation publication is not bypassed.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07i-create-c4-diagram`
- Title: `Migrate create-C4-diagram workflow to shared skill +semver: skip`
- Base: `main`

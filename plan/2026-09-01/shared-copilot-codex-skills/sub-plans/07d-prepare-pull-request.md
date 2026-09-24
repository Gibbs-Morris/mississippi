# Sub-Plan 07d: Prepare-pull-request skill

## Context

- Master plan: `../PLAN.md`
- Issue: #549
- This is sub-plan 07d of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Prepare accurate normal or stacked pull requests from the actual branch diff.

## Scope

- `.agents/skills/prepare-pull-request/SKILL.md`
- PR template and semver/migration reference material
- commit-guardian and PR-manager source rows

## Deployability

- Feature gate: introduce/redirect/optional retire; existing PR practices remain
  available until evidence passes.
- Safe to deploy: produces drafts and metadata, not unauthorized publication.

## Implementation breakdown

1. Inspect merge-base diff, stack/base/dependency metadata, and all changed files.
2. Produce title with correct `+semver` suffix, business value, design,
   use-cases, file manifest, quality evidence, migration notes, and links.
3. Require evidence-backed claims and prevent stale descriptions after pushes.
4. Default publication to draft and require exact target authorization/digest.

## Testing strategy

Use normal/stacked diff fixtures, missing evidence, changed-after-description,
unauthorized repository, stale digest, and secret-redaction scenarios.

## Acceptance criteria

- [ ] Description reflects the complete actual diff.
- [ ] Stack position and dependencies are explicit.
- [ ] No fabricated tests/business claims or unauthorized publication.
- [ ] Primary runtimes and structural checks pass.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07d-prepare-pull-request`
- Title: `Migrate prepare-pull-request workflow to shared skill +semver: skip`
- Base: `main`

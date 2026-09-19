# Sub-Plan 01: Inventory and classification

## Context

- Master plan: `../PLAN.md`
- Issue: #539
- This is sub-plan 01 of 24.

## Dependencies

- Depends on: none
- Plan approval/PR 1 is required before execution.

## Objective

Create the authoritative, reproducible source-to-target migration matrix
without creating skills or changing active guidance.

## Scope

- `.github/instructions/**/*.instructions.md`
- `.github/agents/**/*.agent.md`
- root/maintenance consumers such as `AGENTS.md`, key-principles docs,
  workflows, and completed Cursor history
- `.github/agent-guidance/migration-matrix.json` and its generated view

## Deployability

- Feature gate: no user-visible behavior; inventory only.
- Safe to deploy: no guidance source is removed or redirected.

## Implementation breakdown

1. Capture both issue baselines and a commit-anchored sorted manifest with
   paths, scopes, bytes, lines, SHA-256, scanner version, and command.
2. Add one row per source/content unit with classification, consumer, owner,
   destination, candidate skill, dependency, risk, issue, and evidence.
3. Record #541/#563 as completed historical dispositions.
4. Register protected Orleans/serialization/storage contracts and all
   hard-coded skill-root consumers.
5. Add completeness and orphan checks without mutating source guidance.

## Testing strategy

Run the inventory twice from a clean checkout and compare normalized JSON and
manifest digests. Verify all 51 instructions, 85 agents, retired Cursor
artifacts, and maintenance references are accounted for.

## Acceptance criteria

- [ ] Initial and `ba4d1b93` baselines are both reproducible.
- [ ] Every current source/content unit has exactly one row.
- [ ] No deletion or skill creation occurs in this PR.
- [ ] Runtime identity protections and residual findings are recorded.
- [ ] Matrix and manifest validate on Windows and case-sensitive Linux.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/01-inventory-classification`
- Title: `Inventory repository AI guidance +semver: skip`
- Base: `main`

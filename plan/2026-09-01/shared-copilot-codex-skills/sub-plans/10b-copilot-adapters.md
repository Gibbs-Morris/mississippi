# Sub-Plan 10b: Thin Copilot adapters

## Context

- Master plan: `../PLAN.md`
- Issue: #562
- This is sub-plan 10b of 24.

## Dependencies

- Depends on: 10a; PR 1 and a #545 `go` are required.

## Objective

Reduce Copilot instructions to justified compatibility/path adapters while
preserving the support contract and eliminating duplicate procedures.

## Scope

- `.github/copilot-instructions.md`
- `.github/instructions/**/*.instructions.md`
- Copilot agent routing references and migration documentation
- `.github/agent-guidance/migration-matrix.json`

## Deployability

- Feature gate: redirect first, retire only after bake and rollback evidence.
- Safe to deploy: Copilot adapters remain discoverable and point to the
  canonical skill/AGENTS sources.

## Implementation breakdown

1. Remove procedures now owned by approved skills and invariants now owned by
   AGENTS files.
2. Retain only useful scoped `applyTo` behavior and document every exact-global
   exception.
3. Add stale-root, no-duplicate, reference-closure, and before/after metrics.
4. Publish migration note with paths, ID/version/deprecation, limitations,
   rollback, and old-agent/new-skill mappings.

## Testing strategy

Run all secondary Copilot discovery/smoke tests, structural/stale-reference
checks, before/after metrics, warm/cold/restart checks, and reverse rollback.

## Acceptance criteria

- [ ] No instruction duplicates a canonical skill procedure.
- [ ] Named Copilot surfaces receive the contract's required guidance.
- [ ] Metrics include lines, bytes, files, globals, duplicates, and context.
- [ ] Migration note and fallback are published.
- [ ] `+semver: skip` and runtime-identity exception checks pass.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/10b-copilot-adapters`
- Title: `Reduce Copilot guidance to compatibility adapters +semver: skip`
- Base: `main`

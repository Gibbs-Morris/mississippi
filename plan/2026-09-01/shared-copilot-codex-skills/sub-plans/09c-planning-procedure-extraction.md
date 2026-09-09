# Sub-Plan 09c: Planning and specification procedure extraction

## Context

- Master plan: `../PLAN.md`
- Issue: #559
- This is sub-plan 09c of 24.

## Dependencies

- Depends on: 09a, 07f, 07g, 07h, 07i; PR 1 and a #545 `go` are required.

## Objective

Route reusable research, requirements, ADR, and diagram procedures through
focused skills while preserving orchestration-only behavior.

## Scope

- `research-codebase`, `refine-requirements`, `author-adr`, and
  `create-c4-diagram` skills/references
- planner/specification agent shells, handoffs, and artifacts
- VFE planning/orchestration procedures

## Deployability

- Feature gate: introduce shared procedures before shell reduction.
- Safe to deploy: Product Owner delegation, sequencing, and context isolation
  remain authoritative.

## Implementation breakdown

1. Separate reusable evidence/requirements/design mechanics from delegation,
   sequencing, phase gates, and isolated context.
2. Remove fixed heavyweight process only where measured outcomes permit.
3. Keep required artifacts and CoV evidence reproducible.
4. Update handoffs, allowlists, references, and migration mappings.

## Testing strategy

Run skill activation/output tests plus governed direct-invocation negatives,
artifact schema/lineage checks, handoff graph checks, and rollback.

## Acceptance criteria

- [ ] Planning agents no longer duplicate canonical mechanics.
- [ ] Required artifacts/evidence remain reproducible.
- [ ] Orchestration entry, handoff, completion, and failure conditions are
  explicit.
- [ ] No direct skill invocation bypasses Product Owner governance.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/09c-planning-procedure-extraction`
- Title: `Extract shared planning procedures +semver: skip`
- Base: `main`

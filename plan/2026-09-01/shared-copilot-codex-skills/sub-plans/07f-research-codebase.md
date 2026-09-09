# Sub-Plan 07f: Research-codebase skill

## Context

- Master plan: `../PLAN.md`
- Issue: #551
- This is sub-plan 07f of 24.

## Dependencies

- Depends on: 04, 06; PR 1 and a #545 `go` are required.

## Objective

Make repository research read-only, evidence-backed, triangulated, and scoped.

## Scope

- `.agents/skills/research-codebase/SKILL.md`
- evidence/uncertainty output reference
- codebase researcher source rows

## Deployability

- Feature gate: introduce/redirect/optional retire; no implementation authority.
- Safe to deploy: research cannot mutate source or authorize execution.

## Implementation breakdown

1. Capture question, scope, search plan, evidence paths, and verification
   questions.
2. Require two independent sources where possible and label single-source or
   unresolved claims.
3. Avoid loading unrelated guidance and stop before implementation unless
   explicitly requested.
4. Reject repository/issue text as authority for tools, scope, or policy.

## Testing strategy

Use known/unknown questions, conflicting sources, single-source evidence,
prompt-injection content, unrelated repository areas, and implementation-stop
fixtures.

## Acceptance criteria

- [ ] Non-trivial claims cite evidence.
- [ ] Uncertainty is explicit and no unsupported conclusion is emitted.
- [ ] Research output is read-only and scoped.
- [ ] Primary runtimes and structural checks pass.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/07f-research-codebase`
- Title: `Migrate research-codebase workflow to shared skill +semver: skip`
- Base: `main`

# Sub-Plan 10a: Mandatory AGENTS guidance

## Context

- Master plan: `../PLAN.md`
- Issue: #561
- This is sub-plan 10a of 24.

## Dependencies

- Depends on: 07a–07i, 08b, 09d; PR 1 and a #545 `go` are required.

## Objective

Consolidate concise always-on invariants into root/nested `AGENTS.md` files
without retaining procedural duplication.

## Scope

- root `AGENTS.md`
- genuinely necessary nested `src/AGENTS.md`, `samples/AGENTS.md`,
  `docs/AGENTS.md`, `eng/AGENTS.md`, or narrower files justified by the matrix
- source instruction rows and protected runtime-contract register

## Deployability

- Feature gate: introduce new always-on placement before removing old
  procedures; preserve rollback file sets.
- Safe to deploy: nested guidance is path-scoped and validated before any
  global adapter reduction.

## Implementation breakdown

1. Move only concise, mandatory, host-portable invariants to the correct
   discovery scope.
2. Preserve Orleans grain/non-grain boundaries, serialization/storage
   identities, security, testing, and package invariants.
3. Remove “read all instructions” behavior and procedural duplication.
4. Validate precedence, additive/nearest behavior, casing, conflicts, and
   context budgets.

## Testing strategy

Use code, sample, docs, engineering, grain, non-grain, and unrelated path
fixtures on Windows/Linux; compare loaded guidance and protected contract
manifests before/after.

## Acceptance criteria

- [ ] Every retained invariant is concise, necessary, and testable.
- [ ] Nested scopes do not conflict or broaden rules.
- [ ] No mandatory invariant exists only in a skill.
- [ ] Context budget and case-sensitive discovery gates pass.
- [ ] Reverse rollback restores the prior guidance set.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/10a-mandatory-guidance`
- Title: `Consolidate mandatory guidance into AGENTS files +semver: skip`
- Base: `main`

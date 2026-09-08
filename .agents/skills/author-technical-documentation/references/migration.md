# Migration

## Rules

- This reference **MUST** be applied only when the page is classified as `migration`. Why: Migration pages are versioned operational contracts.
- Migration page titles **MUST** include exact version scope. Why: Readers need immediate clarity about applicability.
- Migration pages **MUST** document the relevant subset of source versions, target versions, mixed-version support, wire compatibility, storage compatibility, serialization compatibility, config renames, default changes, removed APIs, deprecated APIs, and rollout order. Why: Upgrade risk lives in the details.
- Migration pages **MUST** include exact validation steps and **MUST** state whether rollback is possible and what must be backed up first. Why: Successful migration requires proof and contingency.
- Before-and-after code or configuration examples **MUST** be verified. Why: Migration instructions cannot rely on stale examples.
- Migration pages **MUST NOT** blur migration detail into release notes. Why: Readers need a dedicated upgrade surface.

## Default structure

1. exact scope statement
2. `## Who should read this`
3. `## Compatibility summary`
4. `## Breaking changes`
5. `## Required preparation`
6. `## Upgrade sequence`
7. `## Code and configuration changes`
8. `## Data, state, and serialization implications`
9. `## Validation`
10. `## Rollback`
11. `## Related release notes and reference`

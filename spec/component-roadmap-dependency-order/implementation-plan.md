# Implementation Plan

## Step Outline (Revised)

1. Inspect component-roadmap.md to locate all phase tables and catalog entries.
2. Add a Dependencies column to each phase table; list dependencies in build order.
3. Correct ordering violations (e.g., move AnchorPoint to Phase 1 to satisfy MooringLine).
4. Add a Dependency Rules section:
	- Phase 1 explicit build sequence.
	- Cross-phase dependency table.
	- External dependency table (Inlet/Reservoir).
5. Update component catalog entries to include a Dependencies field.
6. Run Docusaurus build to validate the docs build.

## Files To Touch

- docs/Docusaurus/docs/refraction/component-roadmap.md

## Data Model / Config Changes

- None (documentation-only change).

## API / Contract Changes

- None (documentation-only change).

## Observability

- Not applicable.

## Test Plan

- Run: npm run build (from docs/Docusaurus)

## Rollout Plan

- Documentation-only change; no rollout needed.

## Risks + Mitigations

- Risk: Incorrect dependency assumptions. Mitigation: keep dependencies minimal and derived from documented intent; adjust if implementation reveals differences.

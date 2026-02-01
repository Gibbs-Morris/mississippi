# Implementation Plan

## Step Outline (Initial)

1. Inspect component-roadmap.md to locate phase tables and catalog sections.
2. Add Dependencies column to each phase table.
3. Correct any ordering violations (e.g., AnchorPoint vs MooringLine).
4. Add Dependency Rules section with build order and cross-phase dependencies.
5. Update component catalog entries with Dependencies field.
6. Run Docusaurus build to verify.

## Files To Touch

- docs/Docusaurus/docs/refraction/component-roadmap.md

## Test Plan

- Run: npm run build (from docs/Docusaurus)

## Rollout Plan

- Documentation-only change; no rollout needed.

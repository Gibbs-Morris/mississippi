# RFC: Generate Spring Domain Registrations

## Problem
Spring sample uses manual registration wrappers for AddSpringDomain in client/server/silo. These should be source-generated so manual files can be removed.

## Goals
- Generate AddSpringDomain wrappers for Spring client/server/silo using existing generator pipelines.
- Remove manual SpringDomain*Registrations classes.
- Keep Program.cs calls to AddSpringDomain unchanged.
- Ensure generated wrappers include all relevant generated registrations (aggregates, projections, sagas, mappers).
- Keep AddSpringDomain as the single entrypoint for Spring registration in each SDK type.

## Non-Goals
- Change runtime behavior beyond registration wiring.
- Introduce new public APIs outside Spring sample scope unless required for generator support.

## Current State
UNVERIFIED: Spring has manual registration wrappers under Registrations folders.

## Proposed Design
UNVERIFIED: Add generator outputs in each SDK generator to emit AddSpringDomain wrappers for Spring projects only.
UNVERIFIED: Use analyzer config or project metadata to scope Spring-only generation.

## Alternatives
- Keep manual registration wrappers (rejected by request).
- Use a shared handwritten library (rejected by request).

## Security
No auth changes expected.

## Observability
No new logs/metrics expected.

## Compatibility
Sample-only change; behavior should match existing manual wrappers.

## Risks
- Generator scoping could accidentally emit wrappers for non-Spring projects.
- Missing generated methods would break Spring builds.

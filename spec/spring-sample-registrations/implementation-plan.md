# Implementation Plan

## Outline
1. Locate Spring sample domain projects and existing registration patterns.
2. Identify current Inlet source generator outputs and required registrations.
3. Design registration class naming and placement for client/server/silo SDK types.
4. Add registration classes with AddSpringDomain entrypoints and pending source gen attribute.
5. Ensure registrations include all Inlet-generated types (no manual aggregate lists).
6. Update sample startup wiring to use new entrypoints.
7. Add/update tests covering registration composition.
8. Validate compilation and keep docs in sync.

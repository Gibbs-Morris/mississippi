# Implementation Plan (Initial)

## Requirements (from task.md)

- Sagas are aggregates; reuse existing aggregate infrastructure and patterns.
- Discovery MUST use types/attributes, never namespace conventions.
- State MUST be a record; all state changes via events and reducers.
- Generator reuse: saga generators should mirror aggregate equivalents.
- Provide server, client, and silo generation with a consistent DX.

## Constraints

- Follow repository guardrails (logging, DI, zero warnings, CPM, abstractions).
- No new public API breaking changes without approval.

## Assumptions (UNVERIFIED)

- Existing aggregate generators can be cloned/adapted without architectural conflicts.
- SignalR projection mechanisms are available for saga status updates.

## Unknowns

- Exact aggregate generator entry points and extension patterns.
- Current projection subscription patterns and required DTO shape.
- How aggregates handle step/effect registration and runtime orchestration.

## Initial Plan (Draft)

1. Inventory aggregate generator patterns (server/controller, client actions/effects/state/reducers, silo registrations).
2. Inventory aggregate runtime infrastructure patterns (command handlers, reducers, effects, grain hosting, orchestrators).
3. Define saga abstractions in appropriate `*.Abstractions` projects with minimal new primitives.
4. Define saga runtime implementation in main projects, mirroring aggregate patterns.
5. Adapt generators to saga equivalents with minimal divergence.
6. Add sample saga in Spring to validate end-to-end flow.
7. Add L0 tests for new abstractions, runtime, and generators; L2 tests for end-to-end flow.
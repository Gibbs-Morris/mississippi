# Learned

## Repository Facts (initial, UNVERIFIED)

- Aggregate infrastructure lives under src/EventSourcing.Aggregates and related abstractions.
- Generator patterns live under src/Inlet.*.Generators.* and should be mirrored for sagas.
- Existing DI, logging, and serialization conventions are enforced by repository instructions.

## Requirements Captured (from task.md)

- Sagas are aggregates; reuse aggregate infrastructure and patterns.
- Discovery MUST use attributes/types (no namespace conventions).
- State must be records; state changes only via events and reducers.
- Generator reuse: saga generators should mirror aggregate equivalents.

## Evidence To Collect

- Identify aggregate generator files and their patterns (controller, registration, client actions, reducers, state, registrations).
- Identify aggregate infrastructure base classes and patterns for command handlers, reducers, effects, and grain orchestration.
- Identify existing projection/status update patterns (SignalR/Inlet) to reuse for saga status.

## Risks/Constraints (initial, UNVERIFIED)

- Must not use namespace conventions for discovery; use attributes/types only.
- State must be records; all state changes via events and reducers.
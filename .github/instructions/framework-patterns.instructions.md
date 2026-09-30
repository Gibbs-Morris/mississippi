---
applyTo: 'src/**'
---

# Framework Patterns (Mississippi Core)

Governing thought: Framework code prioritizes developer experience and minimizes cognitive overload by exposing consistent primitives—aggregates, projections, commands, events, actions, reducers, effects—throughout. Internal flexibility serves external consistency.

> Drift check: Review existing framework implementations in `src/Reservoir.*/`, `src/DomainModeling.Runtime/`, and `src/Inlet.*/` before adding new patterns.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [complete framework-development policy](../agent-guidance/framework-development.md#framework-development) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.

## Scope and Audience

Contributors building or extending the Mississippi framework under `src/`. This is about building the infrastructure that enables the strict patterns enforced in samples.

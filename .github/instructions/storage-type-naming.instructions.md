---
applyTo: '**/*.cs'
---

# Storage Type Naming with Attributes

Governing thought: Persisted types carry stable, versioned attribute names (`APP.MODULE.NAME.Vn`) so code can be refactored without breaking stored data.

> Drift check: Ensure registries/serialization code you reference is current before changing attributes or registries.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [persisted-identity contracts](../../src/AGENTS.md#persisted-identity) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.

## Scope and Audience

Developers creating or consuming persisted types in event-sourced/storage components.

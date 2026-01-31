# Reservoir Selectors Spec

**Status**: Draft  
**Task Size**: Medium  
**Approval Checkpoint**: Yes (public API changes)

## Task

Add selectors to Reservoir—pure functions that derive computed state from feature states. Design for developer experience first, following existing framework patterns for domain-first modeling with source generation.

## Key Files

| File | Purpose |
|------|---------|
| [learned.md](./learned.md) | Verified repository facts |
| [rfc.md](./rfc.md) | Full RFC with problem, design, and alternatives |
| [verification.md](./verification.md) | Claims and evidence |
| [implementation-plan.md](./implementation-plan.md) | Step-by-step plan |
| [progress.md](./progress.md) | Timestamped log |

## Quick Summary

Selectors provide:

1. **Derived state** — Compute values from one or more feature states
2. **Reusability** — Single source of truth for business calculations
3. **Testability** — Pure functions are trivially testable
4. **Memoization** (optional) — Skip re-renders when derived value unchanged
5. **Composition** — Build complex selectors from simpler ones

## Links

- Current store API: [src/Reservoir.Abstractions/IStore.cs](../../src/Reservoir.Abstractions/IStore.cs)
- Component base: [src/Reservoir.Blazor/StoreComponent.cs](../../src/Reservoir.Blazor/StoreComponent.cs)
- Framework instructions: [.github/instructions/mississippi-framework.instructions.md](../../.github/instructions/mississippi-framework.instructions.md)

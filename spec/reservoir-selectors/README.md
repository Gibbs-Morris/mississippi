# Reservoir Selectors Spec

**Status**: Draft  
**Task Size**: Small-Medium  
**Approval Checkpoint**: Yes (public API changes)

## Task

Add selectors to Reservoir—pure functions that derive computed state from feature states. This PR implements **client-side infrastructure only**; source generation from Domain definitions is a future phase.

## Scope

| In Scope (This PR) | Future PRs |
|--------------------|------------|
| ✅ `SelectorExtensions` in Reservoir.Abstractions | ⏳ Domain→Client source generation |
| ✅ `Select` methods in StoreComponent | ⏳ `[GenerateSelectors]` attribute |
| ✅ Documentation and patterns | ⏳ Memoization utilities |
| ✅ Sample selectors in Spring.Client | ⏳ Purity analyzer |

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
4. **Composition** — Build complex selectors from simpler ones
5. **Generation-ready** — Architecture supports future Domain→Client generation

## Two Selector Sources

| Source | Location | This PR? |
|--------|----------|----------|
| Manual client selectors | `Client/Features/{Feature}/Selectors/` | ✅ Yes |
| Domain→Client selectors | Define in Domain, generate in Client | ⏳ Future |

## Links

- Current store API: [src/Reservoir.Abstractions/IStore.cs](../../src/Reservoir.Abstractions/IStore.cs)
- Component base: [src/Reservoir.Blazor/StoreComponent.cs](../../src/Reservoir.Blazor/StoreComponent.cs)
- Framework instructions: [.github/instructions/mississippi-framework.instructions.md](../../.github/instructions/mississippi-framework.instructions.md)

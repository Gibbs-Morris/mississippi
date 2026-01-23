# Refraction.Abstractions

**Public contracts for the holographic HUD design system.**

## Purpose

This project contains the stable public programming surface for Refraction—interfaces, abstract bases, DTOs, and events that downstream consumers can depend on without taking implementation details.

## Contents

| Namespace | Description |
|-----------|-------------|
| `Mississippi.Refraction.Abstractions` | Core interfaces for theming, focus, motion |
| `Mississippi.Refraction.Abstractions.Theme` | `IRefractionTheme`, token provider contracts |
| `Mississippi.Refraction.Abstractions.Focus` | `IFocusManager`, focus-scope contracts |
| `Mississippi.Refraction.Abstractions.Motion` | `IMotionPreferences`, animation timing |

## Design Principles

1. **Lightweight Dependencies** – Only `Microsoft.Extensions.DependencyInjection.Abstractions`
2. **Stable Contracts** – Interfaces change rarely; implementations evolve freely
3. **Test-Friendly** – All contracts are mockable for unit tests
4. **No Implementation Details** – No Blazor, no Reservoir, no persistence

## Dependency Direction

```text
Refraction.Pages ───► Refraction ───► Refraction.Abstractions
        │                   │                     ▲
        │                   └─────────────────────┤
        └─────────────────────────────────────────┘
```

Consumers reference abstractions; implementations stay internal.

## Adding New Contracts

1. Define the interface in the appropriate namespace
2. Ensure no implementation dependencies leak through
3. Add XML documentation with examples
4. Register default implementations via DI extensions in `Refraction`

## See Also

- `../Refraction/README.md` – Component library implementation
- `../Refraction.Pages/README.md` – Scene-level compositions with Reservoir

# Refraction

Refraction is a disciplined, neo-blue, holographic instrument HUD component library for Blazor. It makes work feel fast, precise, and spatial: a single focused task lives in a primary glass pane while secondary context briefly orbits and disappears.

## Vision

UI that "materializes" and "dematerializes" from an emitter rather than navigating pages. It favors linework, glow, and negative space over heavy blocks, uses motion to explain intent (open/confirm/error/loading), stays input-agnostic and accessible (reticle focus, keyboard-first paths, reduced-motion), remains legible through transparency on noisy backgrounds, and treats OLED longevity as a first-class constraint.

The overall emotion is **calm, technical immersion**: minimal chrome, maximal clarity, and a sense of information being summoned exactly when needed, then fading back into the void.

## Architecture: State Down, Events Up

Refraction components are **pure presentation**. They follow a strict unidirectional data flow:

```text
┌─────────────────────────────────────────────────────┐
│                   PAGE / SCENE                      │
│  (owns state, dispatches actions to Reservoir)      │
└────────────────────────┬────────────────────────────┘
                         │
         STATE ▼         │         ▲ EVENTS
    (via [Parameter])    │    (via EventCallback)
                         │
┌────────────────────────▼────────────────────────────┐
│                    ORGANISMS                         │
│         Pane, SchematicViewer, SmokeConfirm         │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│                    MOLECULES                         │
│           TelemetryStrip, CommandOrbit              │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│                      ATOMS                           │
│    Emitter, Reticle, InputField, ProgressArc, etc.  │
└─────────────────────────────────────────────────────┘
```

### Component Contract

Every Refraction component:

1. **Receives all data via `[Parameter]`** - Components never fetch their own data
2. **Reports all interactions via `EventCallback`** - Components never mutate state
3. **Has zero knowledge of state management** - No Reservoir, no services, no DI
4. **Is fully controlled** - Parent determines what happens on every interaction

### Why This Matters

- **Testability**: Components are trivially testable - no mocking required
- **Reusability**: Works with any state management solution
- **Time-travel debugging**: When used with Reservoir, every interaction is an action
- **Enterprise customization**: Consumers wrap without forking

## Component Categories

| Category | Purpose | Examples |
|----------|---------|----------|
| **Atoms** | Primitive UI elements | Emitter, Reticle, InputField, ProgressArc |
| **Molecules** | Composed atoms | TelemetryStrip, CommandOrbit |
| **Organisms** | Complex compositions | Pane, SchematicViewer, SmokeConfirm |

## Usage

```razor
@* State flows down via parameters *@
<InputField
    Id="task-title"
    Label="Task Title"
    Value="@currentTitle"
    State="@(hasError ? RefractionStates.Error : RefractionStates.Idle)"
    ValueChanged="HandleTitleChanged" />

@code {
    private string currentTitle = "";
    private bool hasError = false;

    // Events flow up - parent decides what happens
    private void HandleTitleChanged(string newValue)
    {
        // Dispatch to Reservoir, validate, etc.
        currentTitle = newValue;
        hasError = string.IsNullOrWhiteSpace(newValue);
    }
}
```

## Related Packages

- **Refraction.Abstractions**: Contracts for theming, focus management, motion preferences
- **Refraction.Pages**: Pre-built scenes with Reservoir integration

## Documentation

- [SPEC.md](SPEC.md) - Full design language specification
- [COMPONENTS.md](COMPONENTS.md) - Component anatomy and parameters

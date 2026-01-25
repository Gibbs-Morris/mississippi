---
sidebar_position: 8
title: Refraction
description: Holographic HUD component library for Mississippi Blazor applications
---

Refraction is a disciplined, neo-blue, holographic instrument HUD component
library for Blazor. It makes work feel fast, precise, and spatial: a single
focused task lives in a primary glass pane while secondary context briefly
orbits and disappears.

## Vision

UI that "materializes" and "dematerializes" from an emitter rather than
navigating pages. It favors linework, glow, and negative space over heavy
blocks, uses motion to explain intent (open/confirm/error/loading), stays
input-agnostic and accessible (reticle focus, keyboard-first paths,
reduced-motion), remains legible through transparency on noisy backgrounds,
and treats OLED longevity as a first-class constraint.

The overall emotion is **calm, technical immersion**: minimal chrome, maximal
clarity, and a sense of information being summoned exactly when needed, then
fading back into the void.

## Architecture: State Down, Events Up

Refraction components are **pure presentation**. They follow a strict
unidirectional data flow:

- **Pages/Scenes** own state and dispatch actions to Reservoir
- **Organisms** (Pane, SchematicViewer, SmokeConfirm) compose molecules
- **Molecules** (TelemetryStrip, CommandOrbit) compose atoms
- **Atoms** (Emitter, Reticle, InputField, ProgressArc) are primitive elements

Every Refraction component:

1. Receives all data via `[Parameter]` — components never fetch their own data
2. Reports all interactions via `EventCallback` — components never mutate state
3. Has zero knowledge of state management — no Reservoir, no services, no DI
4. Is fully controlled — parent determines what happens on every interaction

## Component Categories

| Category | Purpose | Examples |
|----------|---------|----------|
| **Atoms** | Primitive UI elements | Emitter, Reticle, InputField, ProgressArc |
| **Molecules** | Composed atoms | TelemetryStrip, CommandOrbit |
| **Organisms** | Complex compositions | Pane, SchematicViewer, SmokeConfirm |

## Packages

| Package | Description |
| --- | --- |
| **Refraction.Abstractions** | Contracts for theming, focus management, motion preferences |
| **Refraction** | Component library implementation |
| **Refraction.Pages** | Pre-built scenes with Reservoir integration |

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

## Related Documentation

- [Reservoir](./reservoir/) — Client state management
- [Inlet](./inlet.md) — Projection subscriptions and live updates

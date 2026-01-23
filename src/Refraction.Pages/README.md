# Refraction.Pages

**Scene-level compositions that connect Refraction components to Reservoir state management.**

## Purpose

This library contains "Scenes"—full-page compositions that:

1. Subscribe to Reservoir state via `StoreComponent<TState>`
2. Dispatch actions in response to user interactions
3. Pass state down to pure Refraction components
4. Relay component events up to the store

## Architecture: State Down, Events Up

```text
┌──────────────────────────────────────────────────────────────────┐
│                         Scene (Page)                             │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  StoreComponent<TState> - subscribes to Reservoir store    │  │
│  │                                                            │  │
│  │  State flows DOWN via [Parameter]                          │  │
│  │  ───────────────────────────────────►                      │  │
│  │                                                            │  │
│  │  Events flow UP via EventCallback<T>                       │  │
│  │  ◄───────────────────────────────────                      │  │
│  └────────────────────────────────────────────────────────────┘  │
│         │                                        ▲               │
│         ▼                                        │               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐               │
│  │  Organism   │  │  Organism   │  │  Organism   │               │
│  │  (layout)   │  │  (nav)      │  │  (content)  │               │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘               │
│         │                │                │                      │
│  ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐               │
│  │  Molecules  │  │  Molecules  │  │  Molecules  │               │
│  │  & Atoms    │  │  & Atoms    │  │  & Atoms    │               │
│  └─────────────┘  └─────────────┘  └─────────────┘               │
└──────────────────────────────────────────────────────────────────┘
```

## Key Concepts

### SceneBase

Abstract base class that scenes inherit from. Provides:

- `StoreComponent<TState>` lifecycle integration
- Common scene-level functionality (loading, error states)
- Event handler wiring patterns

### State Contracts

Scenes define their state shape as records:

```csharp
public sealed record DashboardState
{
    public ImmutableList<Widget> Widgets { get; init; } = [];
    public string? SelectedWidgetId { get; init; }
    public bool IsLoading { get; init; }
}
```

### Action Dispatch

When components raise events, scenes dispatch actions:

```csharp
private Task HandleWidgetSelectedAsync(string widgetId)
{
    Store.Dispatch(new SelectWidgetAction(widgetId));
    return Task.CompletedTask;
}
```

## Usage

```razor
@inherits SceneBase<DashboardState>

<DataPane Title="Dashboard" State="@PaneState">
    @foreach (var widget in State.Widgets)
    {
        <StatusCell 
            Label="@widget.Name"
            Value="@widget.Value"
            State="@GetCellState(widget)"
            OnSelected="@(() => HandleWidgetSelectedAsync(widget.Id))" />
    }
</DataPane>
```

## Dependencies

| Project | Purpose |
|---------|---------|
| `Refraction` | Pure UI components (Atoms, Molecules, Organisms) |
| `Refraction.Abstractions` | Contracts for theming, focus, motion |
| `Reservoir` | State management (IStore, IAction, StoreComponent) |

## See Also

- `../Refraction/README.md` – Pure component library
- `../Refraction.Abstractions/README.md` – Public contracts
- `../Reservoir/README.md` – State management patterns

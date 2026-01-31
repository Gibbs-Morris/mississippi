# Learned — Verified Repository Facts

## Current State Access Pattern

- `IStore.GetState<TState>()` returns the full feature state snapshot
- `StoreComponent` subscribes to store changes and re-renders on **any** action dispatch (regardless of which feature changed)
- Components call `GetState<TState>()` in render methods to access current state
- Evidence: [src/Reservoir.Abstractions/IStore.cs](../../src/Reservoir.Abstractions/IStore.cs), [src/Reservoir.Blazor/StoreComponent.cs](../../src/Reservoir.Blazor/StoreComponent.cs)

## Feature State Pattern

- Feature states implement `IFeatureState` with a static `FeatureKey` property
- States are immutable records
- States are registered via `AddReducer` or `AddFeatureState`
- Evidence: [docs/Docusaurus/docs/client-state-management/feature-state.md](../../docs/Docusaurus/docs/client-state-management/feature-state.md)

## Reducer Pattern

- Reducers are pure functions: `(state, action) => newState`
- Registered via `AddReducer<TAction, TState>(func)` or class-based `AddReducer<TAction, TState, TReducer>()`
- Base class: `ActionReducerBase<TAction, TState>`
- Evidence: [docs/Docusaurus/docs/client-state-management/reducers.md](../../docs/Docusaurus/docs/client-state-management/reducers.md)

## Source Generation Pattern

- Domain project defines aggregates, commands, events, projections
- Generators emit Client artifacts: actions, DTOs, feature registrations, mappers
- Attributes trigger generation: `[GenerateAggregateEndpoints]`, `[GenerateCommand]`, `[GenerateProjectionEndpoints]`
- Client features registered via `Add{Aggregate}Feature()` extension methods
- Evidence: [.github/instructions/mississippi-framework.instructions.md](../../.github/instructions/mississippi-framework.instructions.md)

## Project Structure (Spring Sample)

```
Spring.Domain/
├── Aggregates/BankAccount/
│   ├── BankAccountAggregate.cs
│   ├── Commands/
│   ├── Events/
│   └── Reducers/
└── Projections/BankAccountBalance/
    ├── BankAccountBalanceProjection.cs
    └── Reducers/

Spring.Client/
└── Features/
    ├── BankAccountAggregate/   (generated + manual)
    │   ├── Actions/
    │   ├── ActionEffects/
    │   ├── Reducers/
    │   └── State/
    └── EntitySelection/        (pure client-side)
        ├── EntitySelectionState.cs
        └── EntitySelectionReducers.cs
```

## Abstractions Split Pattern

- Public contracts go in `*.Abstractions` projects
- Implementations go in main projects
- Consumers reference abstractions unless implementation is required
- Evidence: [.github/instructions/abstractions-projects.instructions.md](../../.github/instructions/abstractions-projects.instructions.md)

## UNVERIFIED

- Exact memoization requirements or performance pain points
- Whether any existing sample uses computed properties on feature state as pseudo-selectors
- Subscription granularity in Store (appears to be all-or-nothing based on interface)

# Task 7.2: Core Store Implementation

**Status**: ⬜ Not Started  
**Depends On**: 7.1 Abstractions

## Goal

Create the `Ripples` core project with state management logic shared between Server and Client.

## Acceptance Criteria

- [ ] `RippleStore` implementation
- [ ] Built-in reducers for projection state
- [ ] Built-in reducers for command state
- [ ] Selector infrastructure
- [ ] Project targets `net9.0` (inherited from Directory.Build.props)
- [ ] L0 tests for all reducers and selectors

## New Project

`src/Ripples/Ripples.csproj`

## Key Components

- `RippleStore` - Central state container
- `ProjectionReducer` - Handles projection state updates
- `CommandReducer` - Handles command lifecycle state
- `RipplePool<T>` - Manages tiered subscriptions for composable projections

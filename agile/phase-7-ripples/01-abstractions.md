# Task 7.1: Ripples Abstractions

**Status**: ⬜ Not Started  
**Depends On**: 7.0 Architecture Design

## Goal

Create the `Ripples.Abstractions` project containing core interfaces and types shared between Server and Client implementations.

## Acceptance Criteria

- [ ] `IRipple<T>` interface defined
- [ ] `IRipplePool<T>` interface defined
- [ ] `IRippleStore` interface defined
- [ ] Action types: `SubscribeTo<T>`, `Unsubscribe<T>`, `SendCommand<T>`
- [ ] State types: `IProjectionState<T>`, `RipplePoolStats`
- [ ] Project targets `net9.0` (inherited from Directory.Build.props)
- [ ] Minimal dependencies (only what's needed for interfaces)
- [ ] XML documentation on all public types
- [ ] L0 tests for any logic in abstractions

## New Project

`src/Ripples.Abstractions/Ripples.Abstractions.csproj`

## Interface Definitions

See [00-architecture.md](./00-architecture.md) for full interface specifications.

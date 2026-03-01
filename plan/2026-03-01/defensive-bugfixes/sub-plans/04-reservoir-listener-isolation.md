# Sub-Plan 04: Reservoir Listener Isolation

## Context

- Master plan: `/plan/2026-03-01/defensive-bugfixes/PLAN.md`
- This is sub-plan 04 of 04

## Dependencies

- Depends on: none
- PR 1 (plan commit) must be merged before execution

## Objective

Isolate each subscriber/listener invocation in `Store.NotifyListeners()` and `StoreEventSubject<T>.OnNext()` with try-catch blocks, so one throwing subscriber cannot prevent other subscribers from receiving notifications.

## Scope

### Files modified

- `src/Reservoir.Core/Store.cs` — wrap each listener invocation in `NotifyListeners()` with try-catch
- `src/Reservoir.Core/StoreEventSubject.cs` — wrap each observer invocation in `OnNext()` with try-catch, calling `observer.OnError(ex)` per Rx pattern

### Test files modified/created

- `tests/Reservoir.Core.L0Tests/StoreTests.cs` — add tests for listener isolation
- `tests/Reservoir.Core.L0Tests/StoreEventSubjectTests.cs` — add tests for observer isolation

## Deployability

- Feature gate: None needed — matches expected Reactive Extensions behavior
- Safe to deploy: Improves resilience without changing success-path behavior

## Implementation Breakdown

### Store.NotifyListeners() Isolation

Change from:
```csharp
foreach (Action listener in snapshot)
{
    listener();
}
```

To:
```csharp
foreach (Action listener in snapshot)
{
    try
    {
        listener();
    }
    catch (Exception)
    {
        // Isolate listener failures to prevent one buggy listener
        // from breaking the entire notification chain.
    }
}
```

### StoreEventSubject<T>.OnNext() Isolation

Change from:
```csharp
foreach (IObserver<T> observer in snapshot)
{
    observer.OnNext(value);
}
```

To:
```csharp
foreach (IObserver<T> observer in snapshot)
{
    try
    {
        observer.OnNext(value);
    }
    catch (Exception ex)
    {
        // Match Reactive Extensions pattern: notify observer of error
        // without breaking other observers.
        try
        {
            observer.OnError(ex);
        }
        catch (Exception)
        {
            // Observer.OnError itself threw — nothing more we can do.
        }
    }
}
```

### Tests

1. **Store listener isolation test**: Register 3 listeners — first succeeds, second throws, third succeeds. Dispatch an action. Assert first and third were both called.
2. **StoreEventSubject observer isolation test**: Subscribe 3 observers — first succeeds, second throws on OnNext, third succeeds. Publish a value. Assert first and third both received the value, and second received OnError.
3. **Existing tests**: Verify all existing Store and StoreEventSubject tests still pass.

## Testing Strategy

- L0 tests only
- Key assertion: throwing subscriber does NOT prevent subsequent subscribers from executing
- Must pass mutation testing

## Acceptance Criteria

- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own
- [ ] A throwing listener in `Store.Subscribe` does not prevent other listeners from executing
- [ ] A throwing observer in `StoreEventSubject.Subscribe` does not prevent other observers from executing
- [ ] StoreEventSubject calls `OnError` on the failing observer (Rx pattern)
- [ ] Existing Store and StoreEventSubject tests pass without modification
- [ ] Mutation tests pass

## PR Metadata

- Branch: `epic/defensive-bugfixes/04-reservoir-listener-isolation`
- Title: `Isolate store listener exceptions to prevent dispatch chain breakage +semver: fix`
- Base: `main`

## Decomposition Guardrails Applied

- Changes are isolated to Reservoir.Core — no cross-project dependencies
- No storage or serialization changes
- Behavior change is strictly additive: existing success paths unchanged; only failure paths are now isolated
- Exception swallowing precedent: `Store.HandleEffectsAsync()` at L430-437 already swallows exceptions in the same file — this pattern is established in the codebase
- Silent catch (no logging) is acceptable here: `Store` has no `ILogger` dependency and adding one would expand scope beyond bug fixing

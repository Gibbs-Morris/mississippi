# Implementation Plan

## Overview

Refactor in 6 small, independently-testable steps. Each step:
1. Makes one focused change
2. Adds/updates tests
3. Verifies build + tests pass
4. Commits

## Step 1: Extract `ProjectionActionFactory`

**Justification**: 
- DRY - consolidates 4 identical `Activator.CreateInstance` patterns
- Testable in isolation
- No behavior change, pure extraction

**Changes**:
1. Create `ProjectionActionFactory.cs` with 4 methods
2. Update `InletSignalREffect` to use the factory
3. Add tests for `ProjectionActionFactory`
4. Verify existing tests still pass

**Commit**: `refactor(inlet): extract ProjectionActionFactory from InletSignalREffect`

---

## Step 2: Extract `IHubConnectionProvider`

**Justification**:
- Testability - this is the main blocker for L0 testing
- SRP - separates connection management from effect logic

**Changes**:
1. Create `IHubConnectionProvider.cs` interface
2. Create `HubConnectionProvider.cs` implementation
3. Update `InletSignalREffect` constructor to accept `IHubConnectionProvider`
4. Update DI registration
5. Add tests for `HubConnectionProvider`
6. Update `InletSignalREffectTests` to use mock provider

**Commit**: `refactor(inlet): extract IHubConnectionProvider for testability`

---

## Step 3: Replace `IServiceProvider` with `Lazy<IInletStore>`

**Justification**:
- Explicit dependencies are better than service locator
- Standard DI pattern for circular dependency resolution

**Changes**:
1. Change constructor parameter from `IServiceProvider` to `Lazy<IInletStore>`
2. Update DI registration to provide `Lazy<IInletStore>`
3. Remove `serviceProvider` field
4. Update tests

**Commit**: `refactor(inlet): use Lazy<IInletStore> instead of IServiceProvider`

---

## Step 4: Extract private fetch helper

**Justification**:
- DRY - fetch-with-error-handling pattern repeated 3×
- Keeps logic in same class (KISS) but reduces duplication

**Changes**:
1. Create private `FetchProjectionAsync` helper method
2. Update `HandleRefreshAsync`, `HandleSubscribeAsync`, `OnProjectionUpdatedAsync` to use it
3. Verify existing tests still pass

**Commit**: `refactor(inlet): extract FetchProjectionAsync helper to reduce duplication`

---

## Step 5: Add comprehensive L0 tests

**Justification**:
- Now that `HubConnection` is mockable, we can test all code paths
- Increases coverage significantly

**Changes**:
1. Add tests for `HandleSubscribeAsync` with mocked hub
2. Add tests for `HandleUnsubscribeAsync` with mocked hub
3. Add tests for `HandleRefreshAsync` with mocked hub
4. Add tests for `OnProjectionUpdatedAsync` callback
5. Add tests for `OnReconnectedAsync` callback
6. Add tests for error scenarios

**Commit**: `test(inlet): add comprehensive L0 tests for InletSignalREffect`

---

## Step 6: Final cleanup and documentation

**Justification**:
- Ensure code style compliance
- Update XML docs if needed

**Changes**:
1. Run StyleCop/ReSharper cleanup
2. Fix any remaining warnings
3. Final build and test
4. Push

**Commit**: `chore(inlet): cleanup after InletSignalREffect refactoring`

---

## Rollback Strategy

Each step is a separate commit. If any step fails:
1. `git reset --hard HEAD~1` to undo the last commit
2. Fix the issue
3. Re-apply the step

## Success Criteria

- [ ] All existing tests pass
- [ ] All new tests pass
- [ ] Zero build warnings
- [ ] `InletSignalREffect` reduced from ~486 lines to ~250-300 lines
- [ ] Test coverage for this file increases from ~160 uncovered to <50 uncovered

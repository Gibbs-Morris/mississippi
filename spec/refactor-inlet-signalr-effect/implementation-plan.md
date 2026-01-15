# Implementation Plan

## Overview

Refactor in focused steps. Each step:
1. Makes one focused change
2. Adds/updates tests
3. Verifies build + tests pass
4. Commits

## Step 1: Extract `ProjectionActionFactory` ✅ COMPLETE

**Justification**: 
- DRY - consolidates 4 identical `Activator.CreateInstance` patterns
- Testable in isolation
- No behavior change, pure extraction

**Changes**:
1. Create `ProjectionActionFactory.cs` with 4 methods (as static class per analyzer requirements)
2. Update `InletSignalREffect` to use the factory
3. Add 15 tests for `ProjectionActionFactory`
4. Verify existing tests still pass (120 tests)

**Commit**: `91ae178 refactor(inlet): extract ProjectionActionFactory from InletSignalREffect`

---

## Step 2: Extract `IHubConnectionProvider` ✅ COMPLETE

**Justification**:
- Testability - this is the main blocker for L0 testing
- SRP - separates connection management from effect logic

**Changes**:
1. Create `IHubConnectionProvider.cs` interface
2. Create `HubConnectionProvider.cs` implementation
3. Update `InletSignalREffect` constructor to accept `IHubConnectionProvider`
4. Update DI registration
5. Rewrite `InletSignalREffectTests` to use mock provider
6. Add 2 constructor registration tests

**Commit**: `1dffcb0 refactor(inlet): extract IHubConnectionProvider for testability`

---

## Step 3: Replace `IServiceProvider` with `Lazy<IInletStore>` ❌ SKIPPED

**Original Justification**:
- Explicit dependencies are better than service locator
- Standard DI pattern for circular dependency resolution

**Decision**: SKIP - After verification:
- `Lazy<T>` is NOT a standard .NET DI pattern (requires manual registration)
- `IServiceProvider` is the common pattern for breaking circular dependencies
- Current pattern already provides adequate testability (mock store registered in test service provider)
- Adding `Lazy<IInletStore>` factory registration adds complexity without meaningful benefit
- Per "keep solutions minimal" directive, this is overengineering

---

## Step 4: Extract private fetch helper ❌ SKIPPED

**Original Justification**:
- DRY - fetch-with-error-handling pattern repeated 3×

**Decision**: SKIP - After verification:
- Three fetch patterns differ significantly:
  - `HandleRefreshAsync`: yields Loading→Updated/Error via IAsyncEnumerable
  - `HandleSubscribeAsync`: yields Loading→Loaded/Error via IAsyncEnumerable
  - `OnReconnectedAsync`: dispatches directly to Store (not yielding)
- Extracting a common helper would require callback-based design (over-engineering)
- Returning a result type would still require per-caller conversion (still repetitive)
- The repetition is localized and standard C# try/catch
- Per "keep solutions minimal" and "avoid overengineering" directives, this is unnecessary abstraction

---

## Step 5: Handler logic testing ⏭️ DEFERRED TO L2

**Original Plan**:
- Add comprehensive L0 tests for handler logic with mocked hub

**Decision**: DEFER - After analysis:
- `IHubConnectionProvider` exposes `HubConnection` directly
- Testing `HandleAsync` handler logic requires mocking `HubConnection.InvokeAsync`
- `HubConnection` is not easily mockable without additional abstraction
- Full handler testing is appropriate for L2 integration tests with real SignalR connection
- Current L0 tests adequately cover:
  - Constructor validation (4 tests)
  - Constructor behavior (2 tests)
  - `CanHandle` logic (8 tests)
  - `HandleAsync` null check (1 test)
  - `DisposeAsync` (1 test)
- Total: 17 tests for `InletSignalREffect` (plus 15 for `ProjectionActionFactory`)

---

## Step 6: Final cleanup and documentation ⏳ IN PROGRESS

**Changes**:
1. Run StyleCop/ReSharper cleanup
2. Fix any remaining warnings
3. Final build and test
4. Update spec progress
5. Delete spec folder
6. Commit and push

---

## Success Criteria (Revised)

- [x] All existing tests pass (121 tests)
- [x] All new tests pass (32 new tests across factory + effect)
- [x] Zero build warnings
- [x] `InletSignalREffect` is now testable via `IHubConnectionProvider` mock
- [x] DRY improved via `ProjectionActionFactory` extraction
- [ ] Final cleanup run
- [ ] Spec folder deleted and committed

## Rollback Strategy

Each step is a separate commit. If any step fails:
1. `git reset --hard HEAD~1` to undo the last commit
2. Fix the issue
3. Re-apply the step


# Phase 8: Post-Review Fixes

**Status**: ✅ Complete (except L2 infrastructure)

## Goal

Address critical issues identified during the comprehensive review of the Cascade Chat Sample, focusing on proper abstraction boundaries and ensuring real-time functionality works end-to-end.

## Tasks

### P0 – Critical Issues

#### 8.1 AggregateKey Abstraction (Hide BrookKey from Samples)

**Status**: ✅ Complete

**Problem**: `BrookKey` is an infrastructure concept from the Brooks (event sourcing streams) module. Domain and application code should not directly reference `BrookKey`. The samples currently expose this internal concept:

```csharp
// Current (wrong) - exposes infrastructure concern
BrookKey channelKey = BrookKey.ForGrain<IChannelAggregateGrain>(channelId);
```

**Solution**: Create `AggregateKey` in `EventSourcing.Aggregates.Abstractions` that provides the same functionality but hides the `BrookKey` implementation detail:

```csharp
// Target (correct) - domain-level abstraction
AggregateKey channelKey = AggregateKey.ForAggregate<IChannelAggregateGrain>(channelId);
```

**Acceptance Criteria**:

- [x] Create `AggregateKey` readonly record struct in `EventSourcing.Aggregates.Abstractions`
- [x] Add `ForAggregate<TGrain>(string entityId)` static factory method
- [x] Add implicit conversion from `AggregateKey` to `BrookKey` (framework internal use only)
- [x] Update `IAggregateGrainFactory.GetAggregate<T>` to accept `AggregateKey`
- [x] Keep `BrookKey` overload for backward compatibility (internal/framework use)
- [x] Add L0 tests for `AggregateKey` (18 new tests)

---

#### 8.2 Fix ChatService BrookKey Exposure

**Status**: ✅ Complete

**Problem**: `ChatService.cs` and `UserSession.cs` directly used `BrookKey.ForGrain<T>()` throughout, exposing infrastructure to the application layer.

**Solution**: Replace all `BrookKey.ForGrain<T>()` calls with `AggregateKey.ForAggregate<T>()`.

**Affected Files**:

- `samples/Cascade/Cascade.Server/Components/Services/ChatService.cs`
- `samples/Cascade/Cascade.Server/Components/Services/UserSession.cs`

**Acceptance Criteria**:

- [x] Replace all `BrookKey` usages with `AggregateKey`
- [x] Remove `using Mississippi.EventSourcing.Brooks.Abstractions;`
- [x] Zero warnings after change

---

#### 8.3 Wire ChannelView to Real Projections

**Status**: ✅ Complete

**Problem**: `ChannelView.razor.cs` uses placeholder data instead of actual projection subscriptions:

```csharp
// Current (placeholder)
await Task.Delay(100); // Simulate async loading
messages.Clear();
```

**Solution**: Wire the component to actual projections using `IProjectionSubscriber<T>`.

**Acceptance Criteria**:

- [x] Subscribe to `ChannelMessagesProjection` for messages
- [x] Subscribe to `ChannelMemberListProjection` for members
- [x] Implement `IAsyncDisposable` for proper cleanup
- [ ] Real-time updates work when messages are sent (requires L2 infrastructure)
- [ ] Multi-user scenario works (User A sends, User B sees) (requires L2 infrastructure)

---

### P1 – High Priority

#### 8.4 Ripples Integration Decision

**Status**: 🔵 Deferred

**Problem**: Ripples framework was built (133 tests passing) but Cascade doesn't use it. Need to decide whether to:

1. **Option A**: Migrate Cascade to Ripples (`IRipple<T>`, `RippleComponent`)
2. **Option B**: Keep using `IProjectionSubscriber<T>` pattern for Cascade (simpler)

**Decision**: Option B selected for now. Cascade uses the simpler `IProjectionSubscriber<T>` pattern. Ripples remains available for future use or other samples that need its advanced features (client-side state, HOT/WARM/COLD tiers).

**Acceptance Criteria**:

- [x] Decision documented (Option B)
- [ ] If Option A: Cascade components use `RippleComponent` base (N/A)
- [x] If Option B: Document Ripples as available but not required for samples

---

#### 8.5 L2 Test Infrastructure

**Status**: ⚠️ Infrastructure Required

**Problem**: L2 Playwright tests fail because they require running Aspire infrastructure (Cosmos Emulator, Azurite).

**Acceptance Criteria**:

- [ ] Exclude L2 tests from default `./go.ps1` run OR
- [ ] Add documentation for running L2 tests with infrastructure

---

#### 8.7 Fix Pre-existing CS8601 Warnings

**Status**: ✅ Complete

**Problem**: Two CS8601 warnings existed in Ripples.Client and Ripples.Server related to `TryRemove` out parameter discards.

**Fix**: Changed `out SubscriptionGroup _` to `out SubscriptionGroup? _` and `out SubscriptionCollection _` to `out SubscriptionCollection? _` to match `TryRemove`'s nullable out parameter.

**Affected Files**:

- `src/Ripples.Client/SignalRRippleConnection.cs` (line 210)
- `src/Ripples.Server/InProcessProjectionUpdateNotifier.cs` (line 106)

**Acceptance Criteria**:

- [x] Zero warnings after rebuild
- [x] All tests continue to pass

---

### P2 – Medium Priority

#### 8.6 Update Agile Tracker

**Status**: ✅ Complete

**Acceptance Criteria**:

- [x] Update agile/README.md with Phase 8 status
- [x] Mark Phase 6 as reviewed (issues logged in Phase 8)
- [x] Ensure completion criteria reflects actual state

---

## Completion Criteria

- [x] `AggregateKey` abstraction hides `BrookKey` from samples
- [x] ChatService and UserSession use `AggregateKey` exclusively
- [x] ChannelView subscribes to real projections (not placeholders)
- [ ] Real-time multi-user messaging works (requires L2 infrastructure)
- [x] L0/L1 tests pass
- [ ] L2 tests pass (requires running infrastructure)
- [ ] ChatService uses `AggregateKey` exclusively
- [ ] ChannelView shows real projection data (not placeholders)
- [ ] Real-time multi-user messaging works
- [ ] `./go.ps1` passes for both solutions

## References

- BrookKey definition: `src/EventSourcing.Brooks.Abstractions/BrookKey.cs`
- ChatService: `samples/Cascade/Cascade.Server/Components/Services/ChatService.cs`
- ChannelView: `samples/Cascade/Cascade.Server/Components/Organisms/ChannelView.razor.cs`
- Ripples framework: `src/Ripples.*`

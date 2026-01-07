# BrookName Attribute Design Learnings

## Date: 2026-01-07

## Context

While implementing Playwright L2 tests for the Cascade sample app, we discovered a design flaw in how `BrookNameAttribute` works with aggregate grains and the `AggregateKey.ForAggregate<T>()` API.

## Problem Discovered

### Symptom

When running the Cascade app and attempting to login, the following error occurred:

```
Type IUserAggregateGrain does not have a BrookNameAttribute.
Decorate the type with [BrookName("APP", "MODULE", "NAME")] to define its brook identity.
```

### Root Cause

The `AggregateKey.ForAggregate<T>()` method uses the type parameter `T` (which is the **interface** type like `IUserAggregateGrain`) to look up the `[BrookName]` attribute. However, the attribute was:

1. Only allowed on classes via `[AttributeUsage(AttributeTargets.Class)]`
2. Only decorated on the concrete grain implementation (e.g., `UserAggregateGrain`)

This created a mismatch - the API looks for the attribute on the interface, but only the class had it.

## Temporary Fix Applied

We made two changes to unblock testing:

1. **Modified `BrookNameAttribute.cs`**: Changed `AttributeUsage` to allow interfaces:
   ```csharp
   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
   ```

2. **Added `[BrookName]` to all grain interfaces**:
   - `IUserAggregateGrain`: `[BrookName("CASCADE", "CHAT", "USER")]`
   - `IConversationAggregateGrain`: `[BrookName("CASCADE", "CHAT", "CONVERSATION")]`
   - `IChannelAggregateGrain`: `[BrookName("CASCADE", "CHAT", "CHANNEL")]`

## Why This Is Not Ideal

The current approach has several problems:

1. **Duplication**: The same `[BrookName]` must appear on both the interface AND the class, leading to:
   - Copy-paste errors if values don't match
   - Maintenance burden when renaming
   - No compile-time enforcement that they match

2. **Confusion**: It's unclear to developers where the attribute should go - the class? the interface? both?

3. **Violation of DRY**: The brook identity is defined twice for the same aggregate.

## Deeper Issue Found

After fixing the attribute issue, we discovered an even deeper problem - the Orleans Silo is failing to start due to DI resolution failures. The server crashes before it can handle any requests, suggesting there are missing service registrations or infrastructure issues in the Cascade.Server setup.

## Proposed Design Changes

### Option A: Interface-Only Approach

- Keep `[BrookName]` on interfaces only
- Change `BrookNameHelper.GetDefinition()` and `BrookNameHelper.GetBrookNameFromGrain()` to look up from the grain's interface, not the class
- The concrete grain would inherit the brook name from its interface

**Pros**: Single source of truth, clear ownership
**Cons**: Requires infrastructure changes in how grains resolve their brook name

### Option B: Class-Only Approach (Recommended)

- Keep `[BrookName]` on concrete grain classes only (revert interface changes)
- Change `AggregateKey.ForAggregate<T>()` to NOT require the attribute at design time
- Instead, the brook name is resolved at runtime when the grain activates
- Create a registration-based approach where `AddUserAggregate()` also registers a mapping from interface to brook name

**Pros**: No attribute on interfaces, single source of truth, runtime resolution
**Cons**: Requires a mapping registry

### Option C: Convention-Based Approach

- Remove the need for `[BrookName]` entirely for standard cases
- Derive brook name from the grain interface name using conventions:
  - `IUserAggregateGrain` → brook name `User`
  - `IConversationAggregateGrain` → brook name `Conversation`
- Allow `[BrookName]` override for non-standard cases

**Pros**: Zero configuration for common cases
**Cons**: Magic conventions can be hard to discover

## Action Items

1. [ ] Investigate the DI resolution failure causing Silo startup crash
2. [ ] Choose a design option (A, B, or C) for the attribute location
3. [ ] Implement the chosen design in the Mississippi framework
4. [ ] Update all samples to use the new pattern
5. [ ] Add documentation explaining where `[BrookName]` should be placed
6. [ ] Add analyzer/source generator to enforce correct usage

## Files Changed (Temporary Fix)

- `src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs`
- `samples/Cascade/Cascade.Domain/User/IUserAggregateGrain.cs`
- `samples/Cascade/Cascade.Domain/Conversation/IConversationAggregateGrain.cs`
- `samples/Cascade/Cascade.Domain/Channel/IChannelAggregateGrain.cs`

## Related Files Created

- `samples/Cascade/Cascade.L2Tests/Features/DataFlowIntegrityTests.cs` - Playwright tests for full data flow verification (blocked until login works)

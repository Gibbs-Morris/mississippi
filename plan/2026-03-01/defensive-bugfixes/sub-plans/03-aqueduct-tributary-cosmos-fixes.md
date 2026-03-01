# Sub-Plan 03: Aqueduct, Tributary, and Cosmos Fixes

## Context

- Master plan: `/plan/2026-03-01/defensive-bugfixes/PLAN.md`
- This is sub-plan 03 of 04

## Dependencies

- Depends on: none
- PR 1 (plan commit) must be merged before execution

## Objective

Fix null-safety in Aqueduct.Abstractions and Tributary.Abstractions key structs, and add negative `maxRetries` validation to `CosmosRetryPolicy`.

## Scope

### Files modified

**Aqueduct.Abstractions (key null-safety):**
- `src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs` — add `field ?? string.Empty` to `GroupName` and `HubName`
- `src/Aqueduct.Abstractions/Keys/SignalRClientKey.cs` — add `field ?? string.Empty` to `ConnectionId` and `HubName`
- `src/Aqueduct.Abstractions/Keys/SignalRServerDirectoryKey.cs` — add `field ?? string.Empty` to `Value`

**Tributary.Abstractions (key null-safety):**
- `src/Tributary.Abstractions/SnapshotStreamKey.cs` — add `field ?? string.Empty` to `BrookName`, `EntityId`, `ReducersHash`, `SnapshotStorageName`
- (SnapshotKey has no direct string properties — nested SnapshotStreamKey handles it)

**Common.Runtime.Storage.Cosmos (validation):**
- `src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs` — add `ArgumentOutOfRangeException.ThrowIfNegative(maxRetries)` to constructor

### Test files modified/created

- `tests/Aqueduct.Abstractions.L0Tests/` — tests for SignalR key struct defaults
- `tests/Tributary.Abstractions.L0Tests/` — tests for SnapshotStreamKey defaults (create test project if needed)
- `tests/Common.Runtime.Storage.Cosmos.L0Tests/` — test for negative maxRetries validation

## Deployability

- Feature gate: None needed — defensive hardening only
- Safe to deploy: All changes make existing APIs more robust

## Implementation Breakdown

### Aqueduct Key Null-Safety (3 structs, 5 properties)

1. `SignalRGroupKey.GroupName` + `HubName` → `string { get => field ?? string.Empty; }`
2. `SignalRClientKey.ConnectionId` + `HubName` → `string { get => field ?? string.Empty; }`
3. `SignalRServerDirectoryKey.Value` → `string { get => field ?? string.Empty; }`

### Tributary Key Null-Safety (1 struct, 4 properties)

1. `SnapshotStreamKey.BrookName` + `EntityId` + `ReducersHash` + `SnapshotStorageName` → `string { get => field ?? string.Empty; }`

### CosmosRetryPolicy Negative Validation

Add after existing null check in constructor:
```csharp
ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
```

### Tests

- For each key struct: assert `default(Struct).Property == string.Empty`
- For each key struct: assert `default(Struct).ToString()` returns non-null
- For CosmosRetryPolicy: assert `new CosmosRetryPolicy(logger, -1)` throws `ArgumentOutOfRangeException`
- For CosmosRetryPolicy: assert `new CosmosRetryPolicy(logger, 0)` does NOT throw (0 retries = valid, means "try once, no retries")

## Testing Strategy

- L0 tests only
- Check if test projects exist for Aqueduct.Abstractions and Tributary.Abstractions; create if needed
- Must pass mutation testing

## Acceptance Criteria

- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own
- [ ] `default(SignalRGroupKey).GroupName` returns `string.Empty`
- [ ] `default(SignalRGroupKey).HubName` returns `string.Empty`
- [ ] `default(SignalRClientKey).ConnectionId` returns `string.Empty`
- [ ] `default(SignalRClientKey).HubName` returns `string.Empty`
- [ ] `default(SignalRServerDirectoryKey).Value` returns `string.Empty`
- [ ] `default(SnapshotStreamKey).BrookName` returns `string.Empty`
- [ ] `default(SnapshotStreamKey).EntityId` returns `string.Empty`
- [ ] `default(SnapshotStreamKey).ReducersHash` returns `string.Empty`
- [ ] `default(SnapshotStreamKey).SnapshotStorageName` returns `string.Empty`
- [ ] `new CosmosRetryPolicy(logger, -1)` throws `ArgumentOutOfRangeException`
- [ ] `new CosmosRetryPolicy(logger, 0)` does not throw
- [ ] Mutation tests pass

## PR Metadata

- Branch: `epic/defensive-bugfixes/03-aqueduct-tributary-cosmos-fixes`
- Title: `Fix SignalR and Snapshot key null defaults, CosmosRetryPolicy negative validation +semver: fix`
- Base: `main`

## Decomposition Guardrails Applied

- No storage name changes
- No partial contracts
- CosmosRetryPolicy validation is constructor-only — no runtime behavior change for valid inputs

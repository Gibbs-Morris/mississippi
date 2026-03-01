# Sub-Plan 02: DomainModeling Fixes

## Context

- Master plan: `/plan/2026-03-01/defensive-bugfixes/PLAN.md`
- This is sub-plan 02 of 04

## Dependencies

- Depends on: none
- PR 1 (plan commit) must be merged before execution

## Objective

Fix null-safety in DomainModeling.Abstractions key structs, fix `OperationResult` default semantics so `default(OperationResult)` represents success, and add duplicate-name conflict detection to EventTypeRegistry and SnapshotTypeRegistry.

## Scope

### Files modified

**DomainModeling.Abstractions (key null-safety):**
- `src/DomainModeling.Abstractions/AggregateKey.cs` — add `field ?? string.Empty` to `EntityId`
- `src/DomainModeling.Abstractions/UxProjectionKey.cs` — add `field ?? string.Empty` to `EntityId`
- `src/DomainModeling.Abstractions/UxProjectionNotificationKey.cs` — add `field ?? string.Empty` to `ProjectionTypeName`
- `src/DomainModeling.Abstractions/UxProjectionVersionedCacheKey.cs` — add `field ?? string.Empty` to `BrookName` and `EntityId`
- `src/DomainModeling.Abstractions/UxProjectionCursorKey.cs` — add `field ?? string.Empty` to `BrookName` and `EntityId`
- (UxProjectionVersionedKey has no direct string properties — skipped)

**DomainModeling.Abstractions (OperationResult):**
- `src/DomainModeling.Abstractions/OperationResult.cs` — change `Success` to infer success from absence of error fields (IsDefault sentinel). `default(OperationResult)` becomes success.

**DomainModeling.Runtime (registry duplicates):**
- `src/DomainModeling.Runtime/EventTypeRegistry.cs` — add ContainsKey check: if name exists with different type, throw `InvalidOperationException`; if same type, skip silently
- `src/DomainModeling.Runtime/SnapshotTypeRegistry.cs` — same pattern

### Test files modified/created

- `tests/DomainModeling.Abstractions.L0Tests/` — tests for key struct defaults, OperationResult default
- `tests/DomainModeling.Runtime.L0Tests/EventTypeRegistryTests.cs` — tests for duplicate behavior
- `tests/DomainModeling.Runtime.L0Tests/SnapshotTypeRegistryTests.cs` — tests for duplicate behavior

## Deployability

- Feature gate: None needed — bug fixes only
- Safe to deploy: OperationResult semantic change is breaking but pre-1.0. Registry change fails fast on genuine errors.

## Implementation Breakdown

### Key Struct Null-Safety (5 structs, 7 properties)

1. `AggregateKey.EntityId` → `string { get => field ?? string.Empty; }`
2. `UxProjectionKey.EntityId` → `string { get => field ?? string.Empty; }`
3. `UxProjectionNotificationKey.ProjectionTypeName` → `string { get => field ?? string.Empty; }`
4. `UxProjectionVersionedCacheKey.BrookName` + `EntityId` → `string { get => field ?? string.Empty; }`
5. `UxProjectionCursorKey.BrookName` + `EntityId` → `string { get => field ?? string.Empty; }`

Add tests for each: `default(Struct).Property == string.Empty`.

### OperationResult Default Fix (Non-Generic Only)

Change `Success` property from a stored `bool` to a computed property on **non-generic `OperationResult` only**:

```csharp
// Current: stored bool, default(bool) = false
[Id(0)]
public bool Success { get; }

// New: infer from ErrorCode presence
[MemberNotNullWhen(false, nameof(ErrorCode))]
[MemberNotNullWhen(false, nameof(ErrorMessage))]
public bool Success { get => ErrorCode is null; }
```

- Remove `success` parameter from private constructor (or keep for backwards compat internally).
- `Ok()` returns `new(null, null)` — `ErrorCode is null` → `Success = true`.
- `Fail(code, msg)` returns `new(code, msg)` — `ErrorCode is not null` → `Success = false`.
- `default(OperationResult)` — ErrorCode is null → `Success = true`. ✅

**Do NOT apply to `OperationResult<T>`** — the generic variant has `[MemberNotNullWhen(true, nameof(Value))]` which would be violated if `default(OperationResult<T>).Success` were `true` (Value would be null). Leave `OperationResult<T>` with its current stored `Success` bool. Add XML doc warning that `default(OperationResult<T>)` is not a valid result.

**Serialization impact**: Remove `[Id(0)]` from `Success` (it becomes computed, not serialized). ErrorCode remains `[Id(1)]` and ErrorMessage `[Id(2)]`. Pre-1.0 so serialization layout changes are permitted.

**Testing**: Add Orleans serialization round-trip tests for `OperationResult` and `OperationResult<T>` to verify the layout change.

### Registry Duplicate Detection

In both `EventTypeRegistry.Register` and `SnapshotTypeRegistry.Register`, replace:
```csharp
if (nameToType.TryAdd(eventName, eventType))
{
    typeToName.TryAdd(eventType, eventName);
}
```

With (using `GetOrAdd` for concurrency safety — atomic on ConcurrentDictionary):
```csharp
Type registeredType = nameToType.GetOrAdd(eventName, eventType);
if (registeredType != eventType)
{
    throw new InvalidOperationException(
        $"Event name '{eventName}' is already registered to type '{registeredType.FullName}'. Cannot register different type '{eventType.FullName}'.");
}

typeToName.TryAdd(eventType, eventName);
```

This is atomic: `GetOrAdd` either adds the new mapping or returns the existing one. If the existing type differs, we throw. The reverse map `TryAdd` is safe since the eventType→eventName mapping is first-wins.

Also update XML docs on `IEventTypeRegistry.Register` and `ISnapshotTypeRegistry.Register` to document the duplicate-name behavior.

## Testing Strategy

- L0 tests for key struct defaults, OperationResult semantics, registry duplicate behavior
- Verify: same name+same type = no throw; same name+different type = throws InvalidOperationException
- Verify: `default(OperationResult).Success == true`, `Ok().Success == true`, `Fail(...).Success == false`
- Must pass mutation testing

## Acceptance Criteria

- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own
- [ ] `default(AggregateKey).EntityId` returns `string.Empty`
- [ ] `default(UxProjectionKey).EntityId` returns `string.Empty`
- [ ] All other key struct properties return `string.Empty` on default
- [ ] `default(OperationResult).Success == true`
- [ ] `OperationResult.Ok().Success == true`
- [ ] `OperationResult.Fail("E", "msg").Success == false`
- [ ] `default(OperationResult<int>).Success == false` (generic variant unchanged — documented as invalid)
- [ ] `OperationResult<T>` has XML doc warning about default being invalid
- [ ] Orleans serialization round-trip tests pass for OperationResult and OperationResult<T>
- [ ] Registry: same name+type registration does not throw
- [ ] Registry: same name+different type throws `InvalidOperationException`
- [ ] Mutation tests pass

## PR Metadata

- Branch: `epic/defensive-bugfixes/02-domain-modeling-fixes`
- Title: `Fix key null defaults, OperationResult default semantics, registry duplicates +semver: fix`
- Base: `main`

## Decomposition Guardrails Applied

- OperationResult serialization ID change: pre-1.0 so permitted; no persisted data concerns (OperationResult is in-memory/RPC only)
- OperationResult<T> intentionally NOT changed to preserve MemberNotNullWhen contract integrity
- Registry uses ConcurrentDictionary.GetOrAdd for atomic conflict detection — no concurrency regression
- Registry behavior change is a startup-time check — no runtime data impact
- No partial contracts — all interfaces remain fully implemented
- IEventTypeRegistry and ISnapshotTypeRegistry XML docs updated to document duplicate-name behavior

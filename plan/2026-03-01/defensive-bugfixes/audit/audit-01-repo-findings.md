# 01 — Repository Findings

## Finding 1: Composite Key Null-Default Pattern

**Claim**: Multiple `readonly record struct` key types have string properties that become `null` on `default(Struct)`, and some return `null` from `ToString()` or implicit string operators.

**Evidence source 1**: `src/Brooks.Abstractions/BrookKey.cs` (L51-53, L138-139)
- `BrookName` and `EntityId` are `string { get; }` with no initializer.
- `ToString()` delegates to implicit operator: `$"{key.BrookName}{Separator}{key.EntityId}"` — produces `"|"` on default (interpolation converts null to empty).
- However, `AggregateKey` (L87-88) returns `key.EntityId` directly from implicit operator — returns **null** on default.

**Evidence source 2**: Subagent analysis of all 13 key structs confirms:
- **Dangerous (return null)**: `AggregateKey`, `UxProjectionKey`, `SignalRServerDirectoryKey` — their implicit operators return a property directly (not interpolated).
- **Misleading but safe**: `BrookKey`, `BrookRangeKey`, `BrookAsyncReaderKey`, `SnapshotStreamKey`, `SnapshotKey`, `UxProjectionNotificationKey`, `UxProjectionCursorKey`, `UxProjectionVersionedCacheKey`, `SignalRGroupKey`, `SignalRClientKey` — produce non-null but semantically meaningless strings like `"|"`, `":"`.
- **UxProjectionVersionedKey** — has no ToString override; auto-generated record struct ToString is safe.

**Confidence**: High — two independent sources (manual code read + automated scan).

**Impact**: Any code path that calls `ToString()` on a default-constructed key or assigns it to a `string` variable will get `null`, potentially causing `NullReferenceException` downstream. The C# 14 `field` keyword approach (`field ?? string.Empty`) can hardcode empty strings into backing fields without breaking `with` expressions or Orleans serialization.

---

## Finding 2: Store Listener Exception Propagation

**Claim**: `Store.NotifyListeners()` and `StoreEventSubject<T>.OnNext()` iterate subscribers without try-catch isolation, so one failing listener/observer crashes the chain.

**Evidence source 1**: `src/Reservoir.Core/Store.cs` (L338-349)
```csharp
private void NotifyListeners()
{
    List<Action> snapshot;
    lock (listenersLock) { snapshot = [.. listeners]; }
    foreach (Action listener in snapshot) { listener(); }  // No try-catch
}
```

**Evidence source 2**: `src/Reservoir.Core/StoreEventSubject.cs` (L68-73)
```csharp
foreach (IObserver<T> observer in snapshot)
{
    observer.OnNext(value);  // No try-catch
}
```

Both patterns iterate without isolation. The Reactive Extensions `Subject<T>` isolates each observer with try-catch and calls `OnError` if one fails.

**Confidence**: High — code is clear, two sources confirm same pattern.

**Impact**: A single buggy listener can prevent all subsequent listeners from receiving state changes, breaking UI reactivity in Blazor scenarios.

---

## Finding 3: Registry Duplicate Registration

**Claim**: `EventTypeRegistry` and `SnapshotTypeRegistry` use `TryAdd`, which silently ignores duplicate names. If two types share the same `[EventStorageName]`, the first registration wins with no warning.

**Evidence source 1**: `src/DomainModeling.Runtime/EventTypeRegistry.cs` (L46-51)
```csharp
if (nameToType.TryAdd(eventName, eventType))
{
    typeToName.TryAdd(eventType, eventName);
}
```

**Evidence source 2**: `src/DomainModeling.Runtime/SnapshotTypeRegistry.cs` (L46-51) — identical pattern.

**Confidence**: High — both files show identical behavior.

**Impact**: Silent duplicate suppression means a misconfigured application can resolve the wrong CLR type for an event name at runtime, leading to deserialization failures or data corruption. A `ContainsKey` guard with an explicit exception would fail fast at startup.

---

## Finding 4: OperationResult Default Semantics

**Claim**: `default(OperationResult)` has `Success = false` (bool default), which means an uninitialized result looks like a failure.

**Evidence source 1**: `src/DomainModeling.Abstractions/OperationResult.cs` (L62)
```csharp
[Id(0)]
public bool Success { get; }
```
Private constructor takes `bool success` — default struct has `Success = false`.

**Evidence source 2**: Factory methods `Ok()` explicitly sets `true` (L107), `Fail()` sets `false` (L86). No sentinel to distinguish "default" from "explicit failure".

**Confidence**: High.

**Impact**: Code that returns `default(OperationResult)` accidentally (e.g., early return path, uninitialized variable) will be treated as a failure with null error codes, which is confusing.

**Fix approach**: Add an `IsDefault` sentinel check — if Success is false AND ErrorCode is null AND ErrorMessage is null, treat as default. The `Success` property can then return `true` for the default case. This requires an internal `_success` field or changing serialization Id layout.

---

## Finding 5: BrookAsyncReaderKey.Parse(null) Exception Type

**Claim**: `Parse(null)` calls `key.AsSpan()` on null, which throws `NullReferenceException` (not `ArgumentNullException`).

**Evidence source 1**: `src/Brooks.Abstractions/BrookAsyncReaderKey.cs` (L84-88)
```csharp
public static BrookAsyncReaderKey Parse(string key)
{
    ReadOnlySpan<char> span = key.AsSpan();  // NRE if key is null
```

No `ArgumentNullException.ThrowIfNull(key)` guard.

**Evidence source 2**: Contrast with `BrookKey.FromString()` (L117) which correctly has `ArgumentNullException.ThrowIfNull(value)`.

**Confidence**: High.

**Impact**: Callers catching `ArgumentNullException` will miss the error; it's a correctness issue for defensive callers.

---

## Finding 6: CosmosRetryPolicy Negative maxRetries

**Claim**: Constructor accepts negative `maxRetries` with no validation.

**Evidence source 1**: `src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs` (L24-32)
```csharp
public CosmosRetryPolicy(ILogger<CosmosRetryPolicy> logger, int maxRetries = 3)
{
    ArgumentNullException.ThrowIfNull(logger);
    MaxRetries = maxRetries;  // No range check
    Logger = logger;
}
```

**Evidence source 2**: The retry loop (L74-106) uses `attempt <= MaxRetries`, so negative values would skip even the first attempt, causing immediate "all retries exhausted" failure.

**Confidence**: High.

**Impact**: Confusing error behavior; should fail fast with `ArgumentOutOfRangeException`.

---

## Finding 7: BrookPosition Default Ambiguity

**Claim**: `default(BrookPosition)` produces `Value = 0`, indistinguishable from valid position 0.

**Evidence source 1**: `src/Brooks.Abstractions/BrookPosition.cs` (L40)
```csharp
public BrookPosition() => Value = -1;  // Parameterless ctor sets -1
```
But `default(BrookPosition)` bypasses the parameterless constructor (CLR zeroes memory), so `Value = 0`.

**Evidence source 2**: The `NotSet` property (L48) checks `Value == -1`, so it won't detect `default(BrookPosition)` as unset — it looks like "position 0" (a valid first event position).

**Confidence**: High.

**Impact**: Cannot be fixed without breaking Orleans serialization (changing `[Id(0)]` on Value) or event sourcing semantics (position 0 is the first event). Documented as unfixable; consumers should use `new BrookPosition()` instead of `default`.

---

## Cross-Cutting Finding: Scope of Affected Key Structs

13 key structs identified across 4 projects:
- **Brooks.Abstractions**: `BrookKey`, `BrookRangeKey`, `BrookAsyncReaderKey`
- **DomainModeling.Abstractions**: `AggregateKey`, `UxProjectionKey`, `UxProjectionNotificationKey`, `UxProjectionVersionedKey`, `UxProjectionVersionedCacheKey`, `UxProjectionCursorKey`
- **Tributary.Abstractions**: `SnapshotStreamKey`, `SnapshotKey`
- **Aqueduct.Abstractions**: `SignalRGroupKey`, `SignalRClientKey`, `SignalRServerDirectoryKey`

All share the pattern of `string { get; }` without null-default protection.
